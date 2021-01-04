using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static unsafe class BrainsDll
{
    [DllImport("brains", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string get_last_error();

    [DllImport("brains", CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort build_population_from_config([MarshalAs(UnmanagedType.LPStr)] string path, void** population, ulong* count, ulong* inputs, ulong* outputs);

    [DllImport("brains", CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort load_existing_population([MarshalAs(UnmanagedType.LPStr)] string membersPath, [MarshalAs(UnmanagedType.LPStr)] string configPath, void** population, ulong* count, ulong* inputs, ulong* outputs, ulong* generation);

    [DllImport("brains", CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort evaluate_member(void* population, ulong index, double* inputs, double* outputs);

    [DllImport("brains", CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort evolve_population(void* population, double* fitness);

    [DllImport("brains", CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort drop_population(void* population);

    [DllImport("brains", CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort save_top_n([MarshalAs(UnmanagedType.LPStr)] string path, void* population, double* fitness, ulong n);

    [DllImport("brains", CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort save_all([MarshalAs(UnmanagedType.LPStr)] string path, void* population);

    [DllImport("brains", CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort export_default_config([MarshalAs(UnmanagedType.LPStr)] string path);
}
