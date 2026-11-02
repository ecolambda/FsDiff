// This file is part of FsDiff.
//
// FsDiff is derived in part from DiffSharp: Differentiable Functional Programming - https://DiffSharp.github.io.
//
// Original DiffSharp copyright:
// Copyright (c) 2016-     University of Oxford (Atilim Gunes Baydin <gunes@robots.ox.ac.uk>)
// Copyright (c) 2017-     Microsoft Research, Cambridge, UK (Don Syme <dsyme@microsoft.com>)
// Copyright (c) 2014-     National University of Ireland Maynooth (Barak A. Pearlmutter <barak@pearlmutter.net>)
// Copyright (c) 2014-2016 National University of Ireland Maynooth (Atilim Gunes Baydin)
// This code is licensed under the BSD license (see LICENSE file for details)

module FsDiff.Tests.AD.Float32

open Expecto
open FsDiff
open FsCheck
open FsDiff.AD.Float64

[<Tests>]
let reverseAD =
    testList
        "Reverse AD"
        [

            testCase "sum"
            <| fun _ ->
                let testNums = [| 1. .. 10. |] |> DV
                let g, gV = grad' DV.sum testNums
                let expect = Array.init 10 (fun _ -> 1.) |> DV
                Expect.equal gV expect "Gradient should be flat ones"

            testCase "head"
            <| fun _ ->
                let testNums = [| 1. .. 10. |] |> DV
                let g, gV = grad' (fun x -> x.[0]) testNums
                let expect = Array.init 9 (fun _ -> 0.) |> Array.append [| 1. |] |> DV
                Expect.equal gV expect "Gradient should be 1 at first element and zero elsewhere"

            testCase "sin"
            <| fun _ ->
                let testNums = [| 1. .. 3. |] |> DV
                let g, gV = grad' (fun x -> sin x.[0]) testNums
                let expect = [| float <| cos testNums.[0]; 0.; 0. |] |> DV
                Expect.equal gV expect ""

            testCase "mul"
            <| fun _ ->
                let testNums = [| 1. .. 3. |] |> DV
                let g, gV = grad' (fun x -> x.[0] * x.[0]) testNums
                let expect = [| float (testNums.[0] * 2.); 0.; 0. |] |> DV
                Expect.equal gV expect ""

            testCase "matrix element"
            <| fun _ ->
                let x0 = DV [| 0.2; 0.4; 0.6; 0.8 |]

                let f (x: DV) =
                    let m = DM.init 2 2 (fun i j -> x.[2 * i + j])
                    sin m.[0, 0]

                let _, gradient = grad' f x0
                let expected = [| cos 0.2; 0.; 0.; 0. |]

                Expect.equal (gradient |> DV.toArray |> Array.map float) expected ""


            test "Vector Element Summation Gradient (d/dx (sum X) = [1, 1, 1])" {
                // f(w) = w0 + w1 + w2 -> Grad must be a flat vector of 1.0 values
                let lossFunction (w: DV) = DV.sum w

                let x0 = DV.create 3 5.5
                let _, gradient = grad' lossFunction x0

                let gradElements = gradient |> DV.toArray |> Array.map (fun d -> d.P)

                Expect.sequenceEqual
                    (gradElements |> Array.map float)
                    [| 1.0; 1.0; 1.0 |]
                    "Vector summation gradient must return 1.0 for every active tracking element lane"
            }

        ]

[<Tests>]
let forward =
    testList
        "Forward AD"
        [

            testProperty "Primal Addition Derivative (d/dx (x + c) = 1)"
            <| fun (xVal: float) (cVal: float) ->
                let f (x: D) = x + (D cVal)
                let _, derivative = diff' f (D xVal)

                Expect.floatClose
                    Accuracy.high
                    (float derivative.P)
                    1.0
                    "Scalar constant addition derivative must be exactly 1.0"

            testProperty "Trigonometric Derivative (d/dx (sin x) = cos x)"
            <| fun (xVal: NormalFloat) ->
                let f (x: D) = sin x
                let _, derivative = diff' f (D xVal.Get)
                let expected = cos xVal.Get

                Expect.floatClose
                    Accuracy.high
                    (float derivative.P)
                    expected
                    "Trigonometric sine derivative must equal cosine of the primal input"

        ]


/// Gradient descent algorithm
let optim (f: DV -> D) (steps: int) (eta: float) (x0: DV) : D * D =
    let initialLoss = f x0

    let finalW =
        [ 1..steps ]
        |> List.fold
            (fun currentW _ ->
                let _, g = grad' f currentW
                currentW - (g * (D eta)))
            x0

    let finalLoss = f finalW
    initialLoss, finalLoss


[<Tests>]
let optimTests =
    testList
        "Gradient Descent Convergence Sweeps"
        [

            test "Gradient descent - Loss Minimized" {
                let lossFunction (w: DV) =
                    let x = toDM [ [ 1.0; 0.0 ] ]
                    let Wg = w.[0..1] |> DM.ofDV 2
                    let g: DM = (x * Wg)
                    cos g.[0, 0]

                let initL, finalL = optim lossFunction 10 0.01 (DV.create 2 1.0)

                Expect.isTrue
                    (finalL.P < initL.P)
                    (sprintf "Optimization failure: Loss stalled. Initial: %A, Final: %A" initL.P finalL.P)
            }

            test "Gradient descent (with arrays) - Matrix Convergence" {
                let n = 3

                let lossFunction (w: DV) =
                    let x = DM.init n n (fun i j -> w.[n * i + j])
                    let x' = x.GetSlice(None, None, None, None)
                    cos x'.[0, 0]

                let initL, finalL = optim lossFunction 5 0.01 (DV.create (n * n) 1.0)

                Expect.isTrue
                    (finalL.P < initL.P)
                    (sprintf "Matrix optimization failure: Loss stalled. Initial: %A, Final: %A" initL.P finalL.P)
            }
        ]
