module IntrinsicsTests

open FsDiff.Backend
open Expecto

let coreBackend = NativeCPU.Float64Backend() :> Backend<float>


[<Tests>]
let vectorMathSanity =
    testList
        "1. Core SIMD Vector Math Primitives"
        [

            test "Vector Addition (Add_V_V)" {
                let x = [| 1.0; 2.0; 3.0 |]
                let y = [| 4.0; 5.0; 6.0 |]
                let result = coreBackend.Add_V_V(x, y)
                Expect.sequenceEqual result [| 5.0; 7.0; 9.0 |] "Vector addition arithmetic failed."
            }

            test "Adding an empty vector" {
                let x = [||]
                let y = [| 4.0; 5.0; 6.0 |]
                let result = coreBackend.Add_V_V(Array.empty, y)
                let result2 = coreBackend.Add_V_V(y, Array.empty)
                Expect.sequenceEqual result y "Vector addition arithmetic failed."
                Expect.sequenceEqual result2 y "Vector addition arithmetic failed."
            }

            test "In-Place Vector Addition (Add_V_V_Inplace)" {
                let x = [| 1.0; 1.0; 1.0 |]
                let dest = [| 10.0; 20.0; 30.0 |]
                coreBackend.Add_V_V_Inplace(x, dest)
                Expect.sequenceEqual dest [| 11.0; 21.0; 31.0 |] "In-place vector modification failed."
            }

            test "Vector Scalar Multiplication (Mul_S_V)" {
                let x = [| 2.0; -4.0; 5.5 |]
                let result = coreBackend.Mul_S_V(3.0, x)
                Expect.sequenceEqual result [| 6.0; -12.0; 16.5 |] "Scalar multiplication failed."
            }

            test "Vector Dot Product (Mul_Dot_V_V)" {
                let x = [| 1.0; 3.0; -5.0 |]
                let y = [| 4.0; -2.0; -1.0 |]
                // (1*4) + (3*-2) + (-5*-1) = 4 - 6 + 5 = 3
                let result = coreBackend.Mul_Dot_V_V(x, y)
                Expect.floatClose Accuracy.high result 3.0 "Vector dot product calculation failed."
            }

            test "Vector Maximum Index Detection (SupNorm_V & idamax)" {
                let x = [| 1.0; -9.5; 3.0; 4.5 |]
                // The maximum absolute value is |-9.5| = 9.5 at index position 1 (0-indexed)
                let norm = coreBackend.SupNorm_V(x)
                Expect.floatClose Accuracy.high norm 9.5 "SupNorm absolute maximum calculation failed."
            }
        ]

[<Tests>]
let matrixMathSanity =
    testList
        "2. Core Matrix Math Primitives"
        [

            test "Matrix-Vector Product (Mul_M_V)" {
                // 2x3 Row-Major Matrix
                let m = array2D [ [ 1.0; 2.0; 3.0 ]; [ 4.0; 5.0; 6.0 ] ]
                let v = [| 1.0; 2.0; 3.0 |]
                // Row 0: 1*1 + 2*2 + 3*3 = 1 + 4 + 9 = 14
                // Row 1: 4*1 + 5*2 + 6*3 = 4 + 10 + 18 = 32
                let result = coreBackend.Mul_M_V(m, v)
                Expect.sequenceEqual result [| 14.0; 32.0 |] "Matrix-vector product failed."
            }

            test "Matrix-Matrix Multiplication (Mul_M_M)" {
                let a = array2D [ [ 1.0; 2.0 ]; [ 3.0; 4.0 ] ]
                let b = array2D [ [ 5.0; 6.0 ]; [ 7.0; 8.0 ] ]
                // Row 0, Col 0: 1*5 + 2*7 = 5 + 14 = 19
                // Row 0, Col 1: 1*6 + 2*8 = 6 + 16 = 22
                // Row 1, Col 0: 3*5 + 4*7 = 15 + 28 = 43
                // Row 1, Col 1: 3*6 + 4*8 = 18 + 32 = 50
                let result = coreBackend.Mul_M_M(a, b)
                let expected = array2D [ [ 19.0; 22.0 ]; [ 43.0; 50.0 ] ]

                // Unroll 2D matrices into sequences to assert equality cleanly
                let resSeq =
                    seq {
                        for r in 0..1 do
                            for c in 0..1 do
                                yield result.[r, c]
                    }

                let expSeq =
                    seq {
                        for r in 0..1 do
                            for c in 0..1 do
                                yield expected.[r, c]
                    }

                Expect.sequenceEqual resSeq expSeq "Matrix-matrix multiplication failed."
            }

            test "Matrix Reshaping and Copying Verification" {
                let m = array2D [ [ 1.0; 2.0 ]; [ 3.0; 4.0 ] ]
                // Should flatten a row-major 2D layout matrix cleanly into a single 1D stream block
                let result = coreBackend.ReshapeCopy_MRows_V(m)
                Expect.sequenceEqual result [| 1.0; 2.0; 3.0; 4.0 |] "Matrix flattening transformation failed."
            }
        ]
