﻿[<AutoOpen>]
module Prelude

[<AutoOpen>]
module TestHelpers =
    open FParsec
    open RawQueryParser

    let run p input =
        match run p input with
        | Success (x, _, _) -> Core.Ok x
        | Failure (msg, _, _) -> Core.Error msg

    let Cond x = Filtering.Condition x

    let Comp x = Cond (Filtering.Comparison x)

    let In x = Cond (Filtering.In x)

    let And x = Filtering.And x

    let Or x = Filtering.Or x

