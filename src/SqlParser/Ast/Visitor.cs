using System.Collections.Immutable;
using System.Reflection;

namespace SqlParser.Ast;

public enum ControlFlow
{
    Continue,
    Break
}

public interface IElement
{
    public IElement Visit(Visitor visitor)
    {
        switch (this)
        {
            case Query q:
                {
                    q = visitor.PreVisitQuery(q);
                    q = VisitChildren(q, visitor);
                    return visitor.PostVisitQuery(q);
                }
            case ObjectName o:
                {
                    o = visitor.PreVisitRelation(o);
                    o = VisitChildren(o, visitor);
                    return visitor.PostVisitRelation(o);
                }
            case TableFactor t:
                {
                    t = visitor.PreVisitTableFactor(t);
                    t = VisitChildren(t, visitor);
                    return visitor.PostVisitTableFactor(t);
                }

            case Expression e:
                {
                    e = visitor.PreVisitExpression(e);
                    e = VisitChildren(e, visitor);
                    return visitor.PostVisitExpression(e);
                }

            case Statement s:
                {
                    s = visitor.PreVisitStatement(s);
                    s = VisitChildren(s, visitor);
                    return visitor.PostVisitStatement(s);
                }

            default:
                var element = this;
                var preVisit = visitor.GetCustomPreVisit(GetType());
                if (preVisit != null)
                {
                    element = preVisit(element);
                }
                element = VisitChildren(element, visitor);
                var postVisit = visitor.GetCustomPostVisit(GetType());
                if (postVisit != null)
                {
                    element = postVisit(element);
                }
                return element;
        }
    }
    public IEnumerable<IElement> Descendants()
    {
        var properties = GetVisitableChildProperties(this);
        foreach (var property in properties)
        {
            if (!property.PropertyType.IsAssignableTo(typeof(IElement)))
            {
                continue;
            }

            if (property.GetValue(this) is IElement value)
            {
                yield return value;

                foreach (var child in value.Descendants())
                {
                    yield return child;
                }
            }
        }
    }

    private static T VisitChildren<T>(T element, Visitor visitor) where T : IElement
    {
        var cloneMethod = element.GetType().GetMethod("<Clone>$");
        var properties = GetVisitableChildProperties(element);

        foreach (var property in properties)
        {
            if (!property.PropertyType.IsAssignableTo(typeof(IElement)))
            {
                continue;
            }

            var value = property.GetValue(element);

            if (value == null)
            {
                continue;
            }

            var child = (IElement)value;
            var newOne = child.Visit(visitor);
            if ((object)newOne != child)
            {
                element = (T)cloneMethod.Invoke(element, null);
                property.SetValue(element, newOne);
            }
        }
        return element;
    }

    private static ImmutableDictionary<Type, IReadOnlyList<PropertyInfo>> _propertiesCache = ImmutableDictionary.Create<Type, IReadOnlyList<PropertyInfo>>();

    internal static IReadOnlyList<PropertyInfo> GetVisitableChildProperties(IElement element)
    {
        var elementType = element.GetType();
        var oldCache = _propertiesCache;
        if (oldCache.TryGetValue(elementType, out var result))
        {
            return result;
        }
        while (true)
        {
            var newCache = oldCache.Add(elementType, GetVisitableChildPropertiesCore());
            if(Interlocked.CompareExchange(ref _propertiesCache, newCache, oldCache) == oldCache || _propertiesCache.ContainsKey(elementType))
            {
                break;
            }
            oldCache = _propertiesCache;
            if (oldCache.TryGetValue(elementType, out result))
            {
                return result;
            }
        }
        return _propertiesCache[elementType];

        IReadOnlyList<PropertyInfo> GetVisitableChildPropertiesCore()
        {
            // Public and not static
            var properties = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var decorated = properties.Where(p => p.GetCustomAttribute<VisitAttribute>() != null)
                .OrderBy(p => p.GetCustomAttribute<VisitAttribute>()!.Order)
                .ToList();

            // No decorated properties uses the default visit order.
            // No need to look for additional properties
            if (!decorated.Any())
            {
                return properties;
            }

            // Visit orders are not specified in the constructor; return the decorated list.
            if (decorated.Count == properties.Length)
            {
                return decorated;
            }

            // Although identified as properties, primary constructor parameters 
            // use parameter attributes, not property attributes and must be identified
            // apart from the property list. This find their order and inserts
            // the missing properties into the decorated property list.
            try
            {
                var constructors = elementType.GetConstructors();
                var primaryConstructor = constructors.Single();
                var constructorParams = primaryConstructor.GetParameters();

                var decoratedParameters = constructorParams.Where(p => p.GetCustomAttribute<VisitAttribute>() != null)
                    .OrderBy(p => p.GetCustomAttribute<VisitAttribute>()!.Order)
                    .Select(p => (Property: p, p.GetCustomAttribute<VisitAttribute>()!.Order))
                    .ToList();

                foreach (var param in decoratedParameters)
                {
                    var property = properties.FirstOrDefault(p => p.Name == param.Property.Name);

                    if (property != null)
                    {
                        decorated.Insert(param.Order, property);
                    }
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }

            return decorated;
        }
    }
}

public abstract class Visitor
{
    private Dictionary<Type, Func<IElement, IElement>> _customPreVisit = new();
    private Dictionary<Type, Func<IElement, IElement>> _customPostVisit = new();
    protected void RegisterCustomPreVisit<T>(Func<T, T> visitor) where T : IElement
    {
        _customPreVisit[typeof(T)] = e => visitor((T)e);
    }
    protected void RegisterCustomPostVisit<T>(Func<T, T> visitor) where T : IElement
    {
        _customPostVisit[typeof(T)] = e => visitor((T)e);
    }
    internal Func<IElement, IElement> GetCustomPreVisit(Type type)
    {
        return _customPreVisit.TryGetValue(type, out var visit) ? visit : null;
    }
    internal Func<IElement, IElement> GetCustomPostVisit(Type type)
    {
        return _customPostVisit.TryGetValue(type, out var visit) ? visit : null;
    }
    public virtual Query PreVisitQuery(Query query)
    {
        return query;
    }

    public virtual Query PostVisitQuery(Query query)
    {
        return query;
    }

    public virtual TableFactor PreVisitTableFactor(TableFactor tableFactor)
    {
        return tableFactor;
    }

    public virtual TableFactor PostVisitTableFactor(TableFactor tableFactor)
    {
        return tableFactor;
    }

    public virtual ObjectName PreVisitRelation(ObjectName relation)
    {
        return relation;
    }

    public virtual ObjectName PostVisitRelation(ObjectName relation)
    {
        return relation;
    }

    public virtual Expression PreVisitExpression(Expression expression)
    {
        return expression;
    }

    public virtual Expression PostVisitExpression(Expression expression)
    {
        return expression;
    }

    public virtual Statement PreVisitStatement(Statement statement)
    {
        return statement;
    }

    public virtual Statement PostVisitStatement(Statement statement)
    {
        return statement;
    }

}