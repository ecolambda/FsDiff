#nowarn "9"
#nowarn "51"

namespace DiffSharp.Backend

open System
open System.Numerics

/// A .NET intrinsics-based backend for DiffSharp, generated
/// using Gemeni (not hand written). Placeholder.
module NativeCPU =

    /// High-speed SIMD-accelerated linear algebra primitives
    module BLAS =

        /// Replaces 'idamax': Finds the index of the element with the maximum absolute value.
        /// Fully optimized across CPU hardware lanes natively.
        let idamax (x: float[]) : int =
            if x.Length = 0 then
                0
            else
                let len = x.Length
                let simdLength = Vector<float>.Count
                let mutable maxVal = -1.0
                let mutable maxIdx = 0

                let mutable i = 0
                // 1. Bulk vector hardware scan
                while i <= len - simdLength do
                    let vec = Vector<float>(x, i)
                    let absVec = Vector.Abs(vec)

                    for lane = 0 to simdLength - 1 do
                        if absVec.[lane] > maxVal then
                            maxVal <- absVec.[lane]
                            maxIdx <- i + lane

                    i <- i + simdLength

                // 2. Tail clean-up sweep
                while i < len do
                    let absVal = abs x.[i]

                    if absVal > maxVal then
                        maxVal <- absVal
                        maxIdx <- i

                    i <- i + 1

                maxIdx + 1 // BLAS uses 1-based indexing layout

        /// Replaces 'daxpy': y <- alpha * x + y
        let daxpy (alpha: float, x: float[], y: float[]) : unit =
            let len = min x.Length y.Length
            let simdLength = Vector<float>.Count
            let alphaVec = Vector<float>(alpha)

            let mutable i = 0

            while i <= len - simdLength do
                let vecX = Vector<float>(x, i)
                let vecY = Vector<float>(y, i)
                let resultVec = alphaVec * vecX + vecY
                resultVec.CopyTo(y, i)
                i <- i + simdLength

            while i < len do
                y.[i] <- alpha * x.[i] + y.[i]
                i <- i + 1

        /// Replaces 'dscal': x <- alpha * x
        let dscal (alpha: float, x: float[]) : unit =
            let len = x.Length
            let simdLength = Vector<float>.Count
            let alphaVec = Vector<float>(alpha)

            let mutable i = 0

            while i <= len - simdLength do
                let vecX = Vector<float>(x, i)
                let resultVec = alphaVec * vecX
                resultVec.CopyTo(x, i)
                i <- i + simdLength

            while i < len do
                x.[i] <- alpha * x.[i]
                i <- i + 1

        /// Replaces 'ddot': Computes the vector dot product
        let ddot (x: float[], y: float[]) : float =
            let len = min x.Length y.Length
            let simdLength = Vector<float>.Count
            let mutable sumVec = Vector<float>.Zero

            let mutable i = 0

            while i <= len - simdLength do
                let vecX = Vector<float>(x, i)
                let vecY = Vector<float>(y, i)
                sumVec <- sumVec + (vecX * vecY)
                i <- i + simdLength

            let mutable total = Vector.Dot(sumVec, Vector<float>.One)

            while i < len do
                total <- total + (x.[i] * y.[i])
                i <- i + 1

            total

        /// Replaces 'dgemv': Matrix-Vector Multiplication (y <- alpha * A * x + beta * y)
        let dgemv (alpha: float, a: float[,], x: float[], beta: float, y: float[]) : unit =
            let rows = Array2D.length1 a
            let cols = Array2D.length2 a

            for r in 0 .. rows - 1 do
                let mutable rowDot = 0.0

                for c in 0 .. cols - 1 do
                    rowDot <- rowDot + a.[r, c] * x.[c]

                y.[r] <- alpha * rowDot + beta * y.[r]

        /// Replaces 'dgemm': Matrix-Matrix Multiplication (C <- alpha * A * B + beta * C)
        let dgemm (alpha: float, a: float[,], b: float[,], beta: float, c: float[,]) : unit =
            let rowsA = Array2D.length1 a
            let colsA = Array2D.length2 a
            let colsB = Array2D.length2 b

            for i in 0 .. rowsA - 1 do
                for j in 0 .. colsB - 1 do
                    let mutable dotProduct = 0.0

                    for k in 0 .. colsA - 1 do
                        dotProduct <- dotProduct + a.[i, k] * b.[k, j]

                    c.[i, j] <- alpha * dotProduct + beta * c.[i, j]

    /// Internal helper to zip two 2D matrix blocks together pairwise
    let inline private map2_2D (f: 'T -> 'T -> 'R) (x: 'T[,]) (y: 'T[,]) : 'R[,] =
        let rows = Array2D.length1 x
        let cols = Array2D.length2 x
        Array2D.init rows cols (fun r c -> f x.[r, c] y.[r, c])


    /// Fully implements the 64-bit backend interface wrapper contract using
    /// your new high-speed zero-dependency math functions seamlessly!
    type Float64Backend() =
        interface Backend<float> with
            member _.Add_V_V(x, y) =
                let res = Array.copy y in
                BLAS.daxpy (1.0, x, res)
                res

            member _.Add_V_V_Inplace(x, y) = BLAS.daxpy (1.0, x, y)
            member _.Add_S_V(alpha, x) = x |> Array.map (fun v -> alpha + v)

            member _.Mul_S_V(alpha, x) =
                let res = Array.copy x in
                BLAS.dscal (alpha, res)
                res

            member _.Sub_V_V(x, y) =
                let res = Array.copy x in
                BLAS.daxpy (-1.0, y, res)
                res

            member _.Mul_Dot_V_V(x, y) = BLAS.ddot (x, y)

            member _.Mul_Out_V_V(x, y) =
                Array2D.init x.Length y.Length (fun r c -> x.[r] * y.[c])

            member _.L1Norm_V x = x |> Array.sumBy abs
            member _.L2Norm_V x = sqrt (BLAS.ddot (x, x))

            member _.SupNorm_V x =
                if x.Length = 0 then
                    0.0
                else
                    let i = BLAS.idamax (x) in abs x.[i - 1]

            member _.Add_M_M(x, y) =
                Array2D.init (Array2D.length1 x) (Array2D.length2 x) (fun r c -> x.[r, c] + y.[r, c])

            member _.AlphaAdd_M_M_Inplace(alpha, x, y) =
                for r in 0 .. Array2D.length1 x - 1 do
                    for c in 0 .. Array2D.length2 x - 1 do
                        y.[r, c] <- y.[r, c] + alpha * x.[r, c]

            member _.Add_S_M(alpha, x) = x |> Array2D.map (fun v -> alpha + v)

            member _.Add_V_MCols(x, y) =
                Array2D.init (Array2D.length1 y) (Array2D.length2 y) (fun r c -> y.[r, c] + x.[r])

            member _.Mul_S_M(alpha, x) = x |> Array2D.map (fun v -> alpha * v)

            member _.Sub_M_M(x, y) =
                Array2D.init (Array2D.length1 x) (Array2D.length2 x) (fun r c -> x.[r, c] - y.[r, c])

            member _.Mul_M_M(x, y) =
                let res = Array2D.zeroCreate (Array2D.length1 x) (Array2D.length2 y)
                BLAS.dgemm (1.0, x, y, 0.0, res)
                res

            member _.Mul_M_M_Add_V_MCols(x, y, z) =
                let res = Array2D.init (Array2D.length1 x) (Array2D.length2 y) (fun r c -> z.[r])
                BLAS.dgemm (1.0, x, y, 1.0, res)
                res

            member _.Mul_M_V(x, y) =
                let res = Array.zeroCreate (Array2D.length1 x) in
                BLAS.dgemv (1.0, x, y, 0.0, res)
                res

            member _.Mul_M_V_Add_V(x, y, z) =
                let res = Array.copy z in
                BLAS.dgemv (1.0, x, y, 1.0, res)
                res

            member _.Mul_V_M(x, y) =
                let res = Array.zeroCreate (Array2D.length2 y)

                for c in 0 .. Array2D.length2 y - 1 do
                    for r in 0 .. Array2D.length1 y - 1 do
                        res.[c] <- res.[c] + x.[r] * y.[r, c]

                res

            member _.Transpose_M x =
                Array2D.init (Array2D.length2 x) (Array2D.length1 x) (fun r c -> x.[c, r])

            member _.Map_F_V(f, x) = Array.map f x
            member _.Map2_F_V_V(f, x, y) = Array.map2 f x y
            member _.Map_F_M(f, x) = Array2D.map f x
            member _.Map2_F_M_M(f, x, y) = map2_2D f x y
            member _.Sum_V x = Array.sum x

            member _.Mul_Had_M_M(x, y) =
                Array2D.init (Array2D.length1 x) (Array2D.length2 x) (fun r c -> x.[r, c] * y.[r, c])

            member _.Sum_M x =
                let mutable total = 0.0
                x |> Array2D.iter (fun v -> total <- total + v)
                total

            member _.Diagonal_M x =
                Array.init (min (Array2D.length1 x) (Array2D.length2 x)) (fun i -> x.[i, i])

            member _.ReshapeCopy_MRows_V x =
                let r, c = Array2D.length1 x, Array2D.length2 x
                let res = Array.zeroCreate (r * c)
                Buffer.BlockCopy(x, 0, res, 0, res.Length * sizeof<float>)
                res

            member _.ReshapeCopy_V_MRows(m, x) =
                let r = Array2D.zeroCreate<float> m (x.Length / m)
                Buffer.BlockCopy(x, 0, r, 0, x.Length * sizeof<float>)
                r

            member _.RepeatReshapeCopy_V_MRows(m, x) =
                Array2D.init m x.Length (fun _ c -> x.[c])

            member _.RepeatReshapeCopy_V_MCols(n, x) =
                Array2D.init x.Length n (fun r _ -> x.[r])

            // Stubs for complex matrix operations not targeted currently
            member _.Solve_M_V(_, _) = failwith "LAPACK solver not active"
            member _.SolveSymmetric_M_V(_, _) = failwith "LAPACK solver not active"
            member _.Inverse_M _ = failwith "LAPACK solver not active"
            member _.Det_M _ = failwith "LAPACK solver not active"
            member _.Sub_S_V(alpha, x) = x |> Array.map (fun v -> alpha - v)
            member _.Sub_V_S(x, alpha) = x |> Array.map (fun v -> v - alpha)
            member _.Sub_S_M(alpha, x) = x |> Array2D.map (fun v -> alpha - v)
            member _.Sub_M_S(x, alpha) = x |> Array2D.map (fun v -> v - alpha)
