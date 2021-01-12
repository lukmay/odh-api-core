﻿module RawQueryParser.Parser

open FParsec

/// Type alias to simplify the type annotation of a Parser.
type Parser<'a> = Parser<'a, unit>

/// <summary>
/// Parse a field path.
/// <c>Detail.de.Title => Field ["Detail"; "de"; "Title"]</c>
/// </summary>
let field =
    let options = IdentifierOptions()
    sepBy (
        identifier options <|> (pint32 |>> string)
    ) (skipChar '.')
    |>> Field
    <?> "field"

let whitespace = many (skipChar ' ')

module Sorting =
    open Sorting

    /// <summary>
    /// Parse a sort direction.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>If omitted the sort is ascending</item>
    /// <item>If <c>-</c> the sort is descending</item>
    /// </list>
    /// </remarks>
    let orderBy: Parser<_> =
        opt (skipString "-")
        |>> function
            | Some _ -> Descending
            | None -> Ascending
        <?> "order by"

    /// A sortStatement consists of a sort direction and a property.
    let sortStatement =
        orderBy .>>. field
        |>> (fun (order, prop) ->
            { Field = prop
              Direction = order })
        <?> "sort statement, e.g. Detail.de.Title"

    /// sortStatements consist of multiple statements divided by a comma.
    let statements: Parser<SortStatements> =
        sepBy1 sortStatement (skipChar ',' >>. whitespace) .>> eof

module Filtering =
    open Filtering

    let betweenBrackets p =
        between (skipChar '(') (skipChar ')') p

    let betweenQuotes p =
        let quotes = skipChar '\"' <|> skipChar '\''
        between quotes quotes p

    let operator: Parser<Operator> =
        choice [
            skipString "eq" >>% Eq
            skipString "ne" >>% Ne
            skipString "gt" >>% Gt
            skipString "ge" >>% Ge
            skipString "lt" >>% Lt
            skipString "le" >>% Le
        ]

    let boolean: Parser<Value> =
        choice [
            skipString "true" >>% Boolean true
            skipString "false" >>% Boolean false
        ]
        <?> "boolean"

    let number: Parser<Value> =
        pfloat |>> Number
        <?> "number"

    let string: Parser<Value> =
        betweenQuotes (
            manySatisfy (isNoneOf ['\"'; '\''])
        )
        |>> String
        <?> "string"

    let value: Parser<Value> =
        choice [
            boolean
            number
            string
        ]

    let call: Parser<Field * Value> =
        betweenBrackets (
            field .>>. (skipChar ',' >>. whitespace >>. value)
        )

    let condition: Parser<Comparison> =
        operator .>>. call
        |>> (fun (op, (prop, value)) ->
            { Field = prop
              Operator = op
              Value = value })
        <?> "condition, e.g. `eq(field, value)`"

    let inParser =
        field .>>. (skipChar ',' >>. whitespace >>. sepBy1 value (skipChar ',' .>> whitespace))

    let statement', statementRef = createParserForwardedToRef<FilterStatement, unit>()

    do statementRef :=
        let innerParser: Parser<FilterStatement list> =
            betweenBrackets (sepBy1 statement' (skipChar ',' >>. whitespace))
        let combineWith f (statements: FilterStatement list) =
            statements |> List.rev |> List.reduce (fun y x -> f (x, y))
        choice [
            pstring "and" >>. innerParser |>> combineWith And
            pstring "or" >>. innerParser |>> combineWith Or
            pstring "isnull" >>. betweenBrackets field |>> (IsNull >> Condition)
            pstring "isnotnull" >>. betweenBrackets field |>> (IsNotNull >> Condition)
            pstring "in" >>. (betweenBrackets inParser |>> (In >> Condition))
            pstring "nin" >>. (betweenBrackets inParser |>> (NotIn >> Condition))
            condition |>> (Comparison >> Condition)
        ]

    let statement = statement' .>> eof

