﻿/// Generates PostgreSQL commands
module RawQueryParser.Writer

/// <summary>
/// Write a field like
/// <c>Field ["Detail"; "de"; "Title"]</c>
/// as
/// <c>data#>'{Detail,de,Title}'</c>
/// </summary>
let writeRawField (Field fields) =
    fields
    |> List.choose (function
        | IdentifierSegment x -> Some x
        | ArraySegment -> None) // Filter away array segment
    |> String.concat ","
    |> sprintf "data#>'\\{%s\\}'"

/// <summary>
/// Write a field like
/// <c>Field ["Detail"; "de"; "Title"]</c>
/// as
/// <c>data#>>'{Detail,de,Title}'</c>
/// </summary>
let writeTextField (Field fields) =
    fields
    |> List.choose (function
        | IdentifierSegment x -> Some x
        | ArraySegment -> None) // Filter away array segment
    |> String.concat ","
    |> sprintf "data#>>'\\{%s\\}'"

module Sorting =
    open Sorting

    /// <summary>
    /// A sort direction gets written
    /// as <c>ASC</c> or <c>DESC</c>.
    /// </summary>
    let writeDirection = function
        | Ascending -> "ASC"
        | Descending -> "DESC"

    /// A statement gets written by concatenating
    /// a property with a sort direction.
    let writeSortStatement statement =
        let direction = writeDirection statement.Direction
        let field = writeRawField statement.Field
        $"{field} {direction}"

    /// Statements are concatenated <see cref=">writeStatement</c>
    let writeStatements (statements: SortStatements) =
        statements
        |> List.map writeSortStatement
        |> String.concat ", "

module Filtering =
    open Filtering

    let writeOperator = function
        | Eq -> "="
        | Ne -> "<>"
        | Gt -> ">"
        | Ge -> ">="
        | Lt -> "<"
        | Le -> "<="

    let writeRawValue = function
        | Boolean x -> box x
        | Number x -> box x
        | String x -> box x
        | DateTime x -> box x
        | Array -> box [||]

    let writeValue = function
        | Boolean true -> "TRUE"
        | Boolean false -> "FALSE"
        | Number value -> $"{value}"
        | String value -> $"'%s{value}'"
        | DateTime value -> $"""'%s{value.ToString("o")}'::timestamp"""
        | Array -> "to_jsonb(array\[\]::varchar\[\])"

    let writeComparison comparison =
        let field =
            match comparison.Value with
            | Boolean _ -> $"({writeRawField comparison.Field})::boolean"
            | Number _ -> $"({writeRawField comparison.Field})::float"
            | String _ -> writeTextField comparison.Field
            | DateTime _ -> $"({writeTextField comparison.Field})::timestamp"
            | Array -> $"({writeRawField comparison.Field})::jsonb"
        let operator = writeOperator comparison.Operator
        let value = writeValue comparison.Value
        $"{field} {operator} {value}"

    let rec writeStatement (jsonSerializer: obj -> string) = function 
        | And (left, right) -> $"(%s{writeStatement jsonSerializer left} AND %s{writeStatement jsonSerializer right})"
        | Or (left, right) -> $"(%s{writeStatement jsonSerializer left} OR %s{writeStatement jsonSerializer right})"
        | Condition (Comparison value) -> writeComparison value
        | Condition (In (field, values)) ->
            let escapeBrackets (x: string) =
                x.Replace("[", "\[")
                 .Replace("]", "\]")
                 .Replace("{", "\{")
                 .Replace("}", "\}")
            let writeValue =
                writeRawValue
                >> field.ToNestedObject
                >> jsonSerializer
                >> escapeBrackets
                >> sprintf "data @> '%s'"
            values
            |> List.map writeValue
            |> String.concat " OR "
            |> sprintf "(%s)"
        | Condition (NotIn (field, values)) ->
            $"NOT {writeStatement jsonSerializer (Condition (In (field, values)))}"
        | Condition (IsNull property) -> $"{writeTextField property} IS NULL"
        | Condition (IsNotNull property) -> $"{writeTextField property} IS NOT NULL"

