FsDiff: F# automatic differentiation for scientific computing
------------------------------------------------

[![.NET](https://github.com/ecolambda/FsDiff/actions/workflows/push.yml/badge.svg)](https://github.com/ecolambda/FsDiff/actions/workflows/push.yml)

FsDiff is an automatic differentiation library for the FsSci ecosystem.

FsDiff provides forward- and reverse-mode automatic differentiation for scientific computing, including simulations, optimisation, and scientific modelling. It has zero dependencies.

---

## NuGet 

Package | Stable | Prerelease
--- | --- | ---
FsDiff | [![NuGet Version](https://img.shields.io/nuget/v/FsDiff)](https://www.nuget.org/packages/fsdiff/) | [![NuGet Version](https://img.shields.io/nuget/vpre/FsDiff)](https://www.nuget.org/packages/fsdiff/)


## Origins

FsDiff incorporates and extends code from the legacy DiffSharp 0.8, keeping the original design based on scalars, vectors, and matrices. For DiffSharp 1.0, the project evolved into a tensor-based architecture for machine-learning workflows. FsDiff instead utilises the original design to focus on scientific computing, for example numerical modelling and optimisation.

DiffSharp was originally developed by Atilim Gunes Baydin and Barak A. Pearlmutter within the Brain and Computation Lab, National University of Ireland Maynooth. The project was later extended through contributions from Don Syme and others.

Original project: https://github.com/DiffSharp/DiffSharp

## License

FsDiff is licensed under the BSD 2-clause license.

FsDiff incorporates code derived from DiffSharp. Original DiffSharp copyright holders include:

University of Oxford (Atilim Gunes Baydin)
Microsoft Research, Cambridge, UK (Don Syme)
National University of Ireland Maynooth (Barak A. Pearlmutter)
National University of Ireland Maynooth (Atilim Gunes Baydin)
See the LICENSE file for full details.