module FsDiffBenchmarks

open Expecto
open FsDiff.AD.Float64


[<BenchmarkDotNet.Attributes.MemoryDiagnoser>]
type PrimalMathBenchmarks() =

    let fScalarA, fScalarB = 1.23, 4.56
    let dScalarA, dScalarB = D 1.23, D 4.56
    let xVec2 = toDV [ 1.0; 1.0 ]
    let xVec100 = toDV (Array.init 100 (fun i -> float i * 0.05))

    // 1. Heavy Compound Expression (mixes trig, exp/log, pow, hyperbolic, division)
    [<Benchmark(Baseline = true)>]
    member __.ComplexExpr_NativeFloat() =
        let x = fScalarA
        let y = fScalarB
        let t1 = exp (-0.5 * x * x) * sin (x * y) + cos (x / y)
        let t2 = tanh (t1 * x) + log (abs (y * t1) + 1.0)
        sqrt (t1 * t1 + t2 * t2) / (1.0 + atan2 t2 t1)

    [<Benchmark>]
    member __.ComplexExpr_PrimalD() =
        let x = dScalarA
        let y = dScalarB
        let t1 = exp (D -0.5 * x * x) * sin (x * y) + cos (x / y)
        let t2 = tanh (t1 * x) + log (abs (y * t1) + D 1.0)
        sqrt (t1 * t1 + t2 * t2) / (D 1.0 + atan2 t2 t1)

    // 2. Sustained Scalar Loop (100 iterations of chaotic map / iterative solver)
    [<Benchmark>]
    member __.Loop100_NativeFloat() =
        let mutable x = fScalarA
        let r = 3.85
        for _ = 1 to 100 do
            x <- r * x * (1.0 - x) + 0.01 * sin x
        x

    [<Benchmark>]
    member __.Loop100_PrimalD() =
        let mutable x = dScalarA
        let r = D 3.85
        let one = D 1.0
        let c = D 0.01
        for _ = 1 to 100 do
            x <- r * x * (one - x) + c * sin x
        x

    [<Benchmark>]
    member __.Grad100() =
        let f (x: DV) = DV.sum (sin x + cos x)
        grad f xVec100

    [<Benchmark>]
    member __.GradientDescentN2() =
        let f (x: DV) = sin x.[0] + cos x.[1]
        CaseStudies.GradientDescent.gd f xVec2 (D 0.9) (D 0.00001)


let benchConfig =
    { benchmarkConfig with diagnosers = [ BenchmarkDotNet.Diagnosers.MemoryDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoserConfig()) ] }

// Uncomment below line to run benchmarks
// [<Tests>]
let tests =
    testList "Performance tests" [

        test "Primal math" {
            benchmark<PrimalMathBenchmarks> benchmarkConfig (fun _ -> null) |> ignore
        }

    ]