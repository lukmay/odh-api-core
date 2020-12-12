﻿module RawQueryParser.Transformer

open FParsec

/// Defines an exception that is thrown when parsing failed.
exception ParserException of string

/// <summary>
/// Transform a sort expression into a
/// PostgreSQL <c>ORDER BY</c> expression.
/// </summary>
[<CompiledName "TransformSort">]
let transformSort input =
    if isNull input then
        nullArg (nameof input)
    else
        match run Parser.Sorting.statements input with
        | Success (statements, _, _) ->
            Writer.Sorting.writeStatements statements
        | Failure (msg, _, _) ->
            raise (ParserException msg)

/// <summary>
/// Transform a filter expression into a
/// PostgreSQL <c>WHERE</c> expression.
/// </summary>
[<CompiledName "TransformFilter">]
let transformFilter input =
    if isNull input then
        nullArg (nameof input)
    else
        match run Parser.Filtering.statement input with
        | Success (statement, _, _) ->
            Writer.Filtering.writeStatement statement
        | Failure (msg, _, _) ->
            raise (ParserException msg)

