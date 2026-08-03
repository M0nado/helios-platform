namespace HELIOS.Analytics.FSharp

open System
open System.Threading
open System.Threading.Tasks

/// Parallel processing helpers for CPU-bound analytics workloads.
module ParallelWorkloads =
    /// Maps work items in parallel while preserving the input order in the returned array.
    let map degreeOfParallelism (work: 'T -> 'U) (items: seq<'T>) =
        if degreeOfParallelism <= 0 then invalidArg (nameof degreeOfParallelism) "Degree of parallelism must be positive."
        let source = items |> Seq.toArray
        let results = Array.zeroCreate<'U> source.Length
        let options = ParallelOptions(MaxDegreeOfParallelism = degreeOfParallelism)
        Parallel.For(0, source.Length, options, fun index -> results[index] <- work source[index]) |> ignore
        results

    /// Reduces partition results in parallel and merges them with the supplied reducer.
    let reduce degreeOfParallelism mapPartition reducePartitions seed (items: seq<'T>) =
        let partitions = items |> Seq.toArray |> Array.chunkBySize (max 1 degreeOfParallelism)
        partitions
        |> map degreeOfParallelism (Array.fold mapPartition seed)
        |> Array.fold reducePartitions seed

    /// Splits work into fixed-size batches for throughput-oriented backends.
    let batches batchSize (items: seq<'T>) =
        if batchSize <= 0 then invalidArg (nameof batchSize) "Batch size must be positive."
        items |> Seq.toArray |> Array.chunkBySize batchSize

    /// Executes asynchronous work with bounded concurrency and cancellation support.
    let mapAsync degreeOfParallelism (cancellationToken: CancellationToken) (work: 'T -> CancellationToken -> Task<'U>) (items: seq<'T>) = task {
        if degreeOfParallelism <= 0 then invalidArg (nameof degreeOfParallelism) "Degree of parallelism must be positive."
        use gate = new SemaphoreSlim(degreeOfParallelism)
        let run item = task {
            do! gate.WaitAsync(cancellationToken)
            try
                cancellationToken.ThrowIfCancellationRequested()
                return! work item cancellationToken
            finally
                gate.Release() |> ignore
        }
        return! items |> Seq.map run |> Task.WhenAll
    }
