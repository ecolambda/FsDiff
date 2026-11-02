module CaseStudies

open FsDiff.AD
open FsDiff.AD.Float64
open Expecto

/// Some test functions adapted from diffsharp 0.8 docs:
module GradientDescent =

    /// Newton's method
    let newton f x0 (eta:D) epsilon =
        let rec desc x =
            let g, h = gradhessian f x
            if DV.l2norm g < epsilon then x else desc (x - eta * (DM.inverse h) * g)
        desc x0

    /// Gradient descent
    let gd f x0 eta epsilon =
        let rec desc x =
            let g = grad f x
            if DV.l2norm g < epsilon then x else desc (x - eta * g)
        desc x0


    [<Tests>]
    let descentTests = testList "Gradient descent" [

        // testCase "Newton's method finds correct minimum of test fn" <| fun _ ->
        //     let f (x:DV) = (exp (x.[0] - 1)) + (exp (- x.[1] + 1)) + ((x.[0] - x.[1]) ** 2)
        //     let xmin = newton f (toDV [0.; 0.]) (D 1.) (D 0.001)
        //     let fxmin = f xmin

        //     Expect.floatClose Accuracy.high (float fxmin) 1.797388803 "minimum was not correct"
            
        //     let xminArr = xmin.ToArray() |> Array.map float
        //     Expect.floatClose Accuracy.high (float fxmin) 1.797388803 "minimum was not correct"
        //     Expect.floatClose Accuracy.high xminArr.[0] 0.7958861818 "x[0] was not correct"
        //     Expect.floatClose Accuracy.high xminArr.[1] 1.203482609 "x[1] was not correct"

        testCase "Identifies minimum" <| fun _ ->
            let f (x:DV) =  sin x.[0] + cos x.[1]
            let xmin = gd f (toDV [1.; 1.]) (D 0.9) (D 0.00001)
            let fxmin = f xmin
            Expect.floatClose Accuracy.high (float fxmin) -2.0 "minimum was not correct"
            
            let xminArr = xmin.ToArray() |> Array.map float
            Expect.floatClose Accuracy.high xminArr.[0] -1.570790759 "minimum was not correct"
            Expect.floatClose Accuracy.high xminArr.[1] 3.141591964 "x[0] was not correct"

    ]


let kinematics =
    testList "Kinematics" [

        test "Position at time t" {
            let x t = t * t * t - 6. * t * t + 10. * t
            let xva = D >> diff2'' (fun t -> t * t * t - 6 * t * t + 10 * t)

            let pos (x, _, _) = x |> float
            let vel (_, v, _) = v |> float
            let acc (_, _, a) = a |> float

            let r = [for t in 0.0..0.01..4.0 -> (t, pos (xva t))]
            printfn "R = %A" r
            ()
        }

    ]