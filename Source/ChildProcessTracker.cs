// -------------------------------------------------------------------------------------
// <copyright file="ChildProcessTracker.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.ComponentModel;
   using System.Diagnostics;
   using System.Runtime.InteropServices;

   /// <summary>Binds a spawned process's lifetime to this one via a Windows Job Object, so
   ///    StrangeLens.Settings.exe (launched separately for Settings/About) doesn't outlive
   ///    StrangeLens.exe as an orphaned window with no tray icon to reopen it from. A handler
   ///    on Tray -> Exit could kill the child explicitly, but that only covers the one exit
   ///    path that runs our own code -- Alt+F4, Task Manager, or a crash would still leave it
   ///    running. The job handle is intentionally never closed here: Windows closes every
   ///    handle a process owns when that process terminates, for any reason, and closing this
   ///    one is exactly what triggers JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE -- so leaving it open
   ///    for the app's entire lifetime is what makes termination-by-any-means reliably take the
   ///    child down with it.</summary>
   internal static class ChildProcessTracker
   {
      private const int JobObjectInfoClassExtendedLimitInformation = 9;

      private const uint JobObjectLimitKillOnJobClose = 0x2000;

      private static readonly IntPtr jobHandle = CreateJob();

      /// <summary>Assigns <paramref name="process"/> to this app's job object, so it's killed when
      ///    this process terminates. Safe to call even if job creation failed -- job object
      ///    support isn't guaranteed on every configuration (e.g., certain sandboxed or heavily
      ///    locked-down environments), and losing this cleanup isn't worth crashing the app over.</summary>
      internal static void Add(Process process)
      {
         if (jobHandle == IntPtr.Zero)
         {
            return;
         }

         try
         {
            if (!AssignProcessToJobObject(jobHandle, process.Handle))
            {
               AppLog.Error(
                  $"ChildProcessTracker: AssignProcessToJobObject failed ({
                     new Win32Exception(Marshal.GetLastWin32Error()).Message}).");
            }
         }
         catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
         {
            // process.Handle can throw if the process already exited before we got here.
            AppLog.Error($"ChildProcessTracker: Add failed: {ex.Message}");
         }
      }

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

      /// <summary>Creates the job object and configures it to kill every process assigned to it as
      ///    soon as the job handle closes. Returns IntPtr.Zero on any failure -- job creation
      ///    itself is not expected to fail under normal conditions, but this is best-effort
      ///    cleanup, not core functionality, so a failure here should degrade quietly rather than
      ///    take the app down.</summary>
      private static IntPtr CreateJob()
      {
         try
         {
            var job = CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero)
            {
               return IntPtr.Zero;
            }

            var info = new JobObjectBasicLimitInformation
               {
                  LimitFlags = JobObjectLimitKillOnJobClose,
               };
            var extendedInfo = new JobObjectExtendedLimitInformation
               {
                  BasicLimitInformation = info,
               };

            var length = Marshal.SizeOf<JobObjectExtendedLimitInformation>();
            var extendedInfoPtr = Marshal.AllocHGlobal(length);
            try
            {
               Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);
               if (!SetInformationJobObject(
                      job,
                      JobObjectInfoClassExtendedLimitInformation,
                      extendedInfoPtr,
                      (uint)length))
               {
                  AppLog.Error(
                     $"ChildProcessTracker: SetInformationJobObject failed ({
                        new Win32Exception(Marshal.GetLastWin32Error()).Message}).");
                  return IntPtr.Zero;
               }
            }
            finally
            {
               Marshal.FreeHGlobal(extendedInfoPtr);
            }

            return job;
         }
         catch (Exception ex)
         {
            AppLog.Error($"ChildProcessTracker: CreateJob failed: {ex.Message}");
            return IntPtr.Zero;
         }
      }

      [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
      private static extern IntPtr CreateJobObject(IntPtr jobAttributes, string? name);

      [DllImport("kernel32.dll", SetLastError = true)]
      private static extern bool SetInformationJobObject(
         IntPtr job,
         int jobObjectInfoClass,
         IntPtr jobObjectInfo,
         uint jobObjectInfoLength);

      [StructLayout(LayoutKind.Sequential)]
      private struct IoCounters
      {
         public ulong ReadOperationCount;

         public ulong WriteOperationCount;

         public ulong OtherOperationCount;

         public ulong ReadTransferCount;

         public ulong WriteTransferCount;

         public ulong OtherTransferCount;
      }

      [StructLayout(LayoutKind.Sequential)]
      private struct JobObjectBasicLimitInformation
      {
         public long PerProcessUserTimeLimit;

         public long PerJobUserTimeLimit;

         public uint LimitFlags;

         public UIntPtr MinimumWorkingSetSize;

         public UIntPtr MaximumWorkingSetSize;

         public uint ActiveProcessLimit;

         public UIntPtr Affinity;

         public uint PriorityClass;

         public uint SchedulingClass;
      }

      [StructLayout(LayoutKind.Sequential)]
      private struct JobObjectExtendedLimitInformation
      {
         public JobObjectBasicLimitInformation BasicLimitInformation;

         public IoCounters IoInfo;

         public UIntPtr ProcessMemoryLimit;

         public UIntPtr JobMemoryLimit;

         public UIntPtr PeakProcessMemoryUsed;

         public UIntPtr PeakJobMemoryUsed;
      }
   }
}
