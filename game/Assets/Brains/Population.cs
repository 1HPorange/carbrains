using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Population : IDisposable
{
    private unsafe void* _population;

    /// <summary>
    /// Number of neural networks in the entire population
    /// </summary>
    public readonly ulong Size;

    /// <summary>
    /// Number of inputs for a single population member
    /// </summary>
    public readonly ulong Inputs;

    /// <summary>
    /// Number of outputs for a single population member
    /// </summary>
    public readonly ulong Outputs;


    private unsafe Population(void* population, ulong size, ulong inputs, ulong outputs)
    {
        _population = population;
        Size = size;
        Inputs = inputs;
        Outputs = outputs;
    }

    public static Population GenerateFromConfig(string path)
    {
        unsafe
        {
            void* population = null;
            void** population_ptr = &population;

            ulong members;
            ulong* members_ptr = &members;

            ulong inputs;
            ulong* inputs_ptr = &inputs;

            ulong outputs;
            ulong* outputs_ptr = &outputs;

            ThrowOnError(() => BrainsDll.build_population_from_config(path, population_ptr, members_ptr, inputs_ptr, outputs_ptr));

            return new Population(population, members, inputs, outputs);
        }
    }

    public static Population LoadFromFile(string membersPath, string configPath)
    {
        unsafe
        {
            void* population = null;
            void** population_ptr = &population;

            ulong members;
            ulong* members_ptr = &members;

            ulong inputs;
            ulong* inputs_ptr = &inputs;

            ulong outputs;
            ulong* outputs_ptr = &outputs;

            ThrowOnError(() => BrainsDll.load_existing_population(membersPath, configPath, population_ptr, members_ptr, inputs_ptr,
                outputs_ptr));

            return new Population(population, members, inputs, outputs);
        }
    }

    public static void ExportDefaultConfig(string path)
    {
        ThrowOnError(() => BrainsDll.export_default_config(path));
    }

    public void EvaluateMember(ulong index, double[] inputs, double[] outputs)
    {
        unsafe
        {
            fixed (double* i = inputs)
            fixed (double* o = outputs)
            {
                var ii = i;
                var oo = o;
                ThrowOnError(() => BrainsDll.evaluate_member(_population, index, ii, oo));
            }
        }
    }

    public void Evolve(double[] fitness)
    {
        unsafe
        {
            fixed (double* f = fitness)
            {
                var ff = f;
                ThrowOnError(() => BrainsDll.evolve_population(_population, ff));
            }
            
        }
    }

    public void SaveTopN(string path, double[] fitness, ulong n)
    {
        unsafe
        {
            fixed (double* f = fitness)
            {
                var ff = f;
                ThrowOnError(() => BrainsDll.save_top_n(path, _population, ff, n));
            }
        }
    }

    public void SaveAll(string path)
    {
        unsafe
        {
            ThrowOnError(() => BrainsDll.save_all(path, _population));
        }
    }

    private static void ThrowOnError(Func<ushort> f)
    {
        if (f() > 0)
        {
            throw new ExternalException($"Native call to brains.dll failed with: {BrainsDll.get_last_error()}");
        }
    }

    private unsafe void ReleaseUnmanagedResources()
    {
        if (null != _population)
        {
            BrainsDll.drop_population(_population);
            _population = null;
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~Population()
    {
        ReleaseUnmanagedResources();
    }
}
