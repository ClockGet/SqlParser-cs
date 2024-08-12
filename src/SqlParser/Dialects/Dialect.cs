﻿using SqlParser.Ast;

namespace SqlParser.Dialects;

/// <summary>
/// Base dialect definition
/// </summary>
public abstract class Dialect
{
    /// <summary>
    /// Determine if a character is the start of an identifier
    /// </summary>
    /// <param name="character">Character to test</param>
    /// <returns>True if the start of an ident; otherwise false.</returns>
    public abstract bool IsIdentifierStart(char character);
    /// <summary>
    /// Determine if a character is the part of an identifier
    /// </summary>
    /// <param name="character">Character to test</param>
    /// <returns>True if part of an ident; otherwise false.</returns>
    public abstract bool IsIdentifierPart(char character);
    /// <summary>
    /// Determine if a character starts a quoted identifier. The default
    /// implementation, accepting "double-quoted" ids is both ANSI-compliant
    /// and appropriate for most dialects (with the notable exception of
    /// MySQL, MS SQL, and sqlite). You can accept one of characters listed
    /// in `Word::matching_end_quote` here
    /// </summary>
    /// <param name="character"></param>
    /// <returns>True if ident start; otherwise false</returns>
    public virtual bool IsDelimitedIdentifierStart(char character)
    {
        return character is Symbols.DoubleQuote or Symbols.Backtick;
    }
    /// <summary>
    /// Determine if quoted characters are proper for identifier
    /// </summary>
    /// <returns>True if proper; otherwise false.</returns>
    public virtual bool IsProperIdentifierInsideQuotes(State state)
    {
        return true;
    }
    /// <summary>
    /// Return the character used to quote identifiers
    /// </summary>
    public virtual char? IdentifierQuoteStyle(string identifier) => null;
    /// <summary>
    /// Allow dialect implementations to override statement parsing
    /// </summary>
    /// <param name="parser">Parser instance</param>
    /// <returns>Parsed Expression</returns>
    public virtual Statement? ParseStatement(Parser parser)
    {
        return null;
    }
    /// <summary>
    /// Allow dialect implementations to override prefix parsing
    /// </summary>
    /// <param name="parser">Parser instance</param>
    /// <returns>Parsed Expression</returns>
    public virtual Expression? ParsePrefix(Parser parser)
    {
        return null;
    }
    /// <summary>
    /// Dialect-specific infix parser override
    /// </summary>
    /// <param name="parser">Parser instance</param>
    /// <param name="expression">Expression</param>
    /// <param name="precedence">Token precedence</param>
    /// <returns>Parsed Expression</returns>
    public virtual Expression? ParseInfix(Parser parser, Expression expression, int precedence)
    {
        return null;
    }
    /// <summary>
    /// Allow dialect implementations to override parser token precedence
    /// </summary>
    /// <param name="parser">Parser instance</param>
    /// <returns>Token Precedence</returns>
    public virtual short? GetNextPrecedence(Parser parser)
    {
        return null;
    }
    /// <summary>
    /// True if the dialect supports filtering during aggregation
    /// </summary>
    /// <returns>True if supported; otherwise false.</returns>
    public virtual bool SupportsFilterDuringAggregation => false;
    /// <summary>
    /// True if the dialect supports `(NOT) in ()`                                
    /// </summary>
    public virtual bool SupportsInEmptyList => false;
    /// <summary>
    /// True if the dialect supports 'group sets, rollup, or cube' expressions
    /// </summary>
    public virtual bool SupportsGroupByExpression => false;
    /// <summary>
    /// Returns true if the dialect supports `SUBSTRING(expr [FROM start] [FOR len])` expressions
    /// </summary>
    public virtual bool SupportsSubstringFromForExpression => true;
    /// <summary>
    /// Returns true if the dialect has a CONVERT function which accepts a type first
    /// and an expression second, e.g. `CONVERT(varchar, 1)`
    /// </summary>
    public virtual bool ConvertTypeBeforeValue => false;
    /// <summary>
    /// Returns true if the dialect supports `BEGIN {DEFERRED | IMMEDIATE | EXCLUSIVE} [TRANSACTION]` statements
    /// </summary>
    public virtual bool SupportsStartTransactionModifier => false;
    /// <summary>
    /// Returns true if the dialect supports function arguments with equals operator
    /// </summary>
    public virtual bool SupportsNamedFunctionArgsWithEqOperator => false;
    /// <summary>
    /// Returns true if the dialect supports backslash escaping
    /// </summary>
    public virtual bool SupportsStringLiteralBackslashEscape => false;
    /// <summary>
    /// Returns true if the dialect supports match recognize
    /// </summary>
    public virtual bool SupportsMatchRecognize => false;
    /// <summary>
    /// Returns true if the dialect supports dictionary syntax
    /// </summary>
    public virtual bool SupportsDictionarySyntax => false;
    /// <summary>
    /// Returns true if the dialect supports connect by syntax
    /// </summary>
    public virtual bool SupportsConnectBy => false;
    public virtual bool SupportsWindowClauseNamedWindowReference => false;
    /// <summary>
    /// Returns true if the dialect supports identifiers starting with a numeric
    /// prefix such as tables named: '59901_user_login'
    /// </summary>
    public virtual bool SupportsNumericPrefix => false;

    public virtual bool SupportsWindowFunctionNullTreatmentArg => false;
    public virtual bool SupportsLambdaFunctions => false;
    public virtual bool SupportsParenthesizedSetVariables => false;
    public virtual bool SupportsTripleQuotedString => false;
    public virtual bool SupportsSelectWildcardExcept => false;
    public virtual bool SupportsTrailingCommas => false;
    public virtual bool SupportsProjectionTrailingCommas => false;
}