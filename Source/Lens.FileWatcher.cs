// -------------------------------------------------------------------------------------
// <copyright file="Lens.FileWatcher.cs">
//   Copyright (c) 2026
//   Licensed under the MIT License. See LICENSE file in the project root.
// </copyright>
// -------------------------------------------------------------------------------------

namespace StrangeLens
{
   using System;
   using System.IO;
   using System.Threading;

   using Timer = System.Timers.Timer;

   public partial class Lens
   {
      private const int ExternalChangeDebounceMs = 150;

      private Timer? externalChangeDebounceTimer;

      private FileSystemWatcher? externalChangeWatcher;

      /// <summary>Captured once, in <see cref="StartWatchingForExternalChanges"/>, and reused
      ///    to marshal both reloads and <see cref="Save"/> onto the same thread. Keeping them
      ///    on one thread is what makes it safe for <see cref="Save"/> to read fields that
      ///    <see cref="SetPersisted{T}"/> writes -- without it, a save's field reads and a
      ///    concurrent edit's field write can race, and a value changed mid-save can get
      ///    written stale and then have its pending flag cleared anyway.</summary>
      private SynchronizationContext? ownerSyncContext;

      /// <summary>Watches settings.json for changes written by another process (e.g. the
      ///    Settings window running as its own process) and reloads when it changes. Call
      ///    from the thread that should receive the reload -- that thread's
      ///    <see cref="SynchronizationContext"/> must already be installed (WinForms:
      ///    after a control handle exists; WinUI 3: any time after <c>OnLaunched</c>).
      ///    Reloading our own just-written file is a no-op: <see cref="SetPersisted{T}"/>
      ///    already skips unchanged values, so this can't feed back into an update
      ///    loop.</summary>
      public void StartWatchingForExternalChanges()
      {
         if (this.externalChangeWatcher != null)
         {
            return;
         }

         this.ownerSyncContext = SynchronizationContext.Current;

         var path = SettingsFilePath;
         var directory = Path.GetDirectoryName(path)!;
         Directory.CreateDirectory(directory);

         this.externalChangeDebounceTimer = new Timer(ExternalChangeDebounceMs) { AutoReset = false };
         this.externalChangeDebounceTimer.Elapsed += (_, _) => this.RunOnOwnerThread(this.Load);

         this.externalChangeWatcher = new FileSystemWatcher(directory, Path.GetFileName(path))
            {
               NotifyFilter = NotifyFilters.LastWrite,
            };
         this.externalChangeWatcher.Changed += (_, _) =>
            {
               this.externalChangeDebounceTimer.Stop();
               this.externalChangeDebounceTimer.Start();
            };
         this.externalChangeWatcher.EnableRaisingEvents = true;
      }

      public void StopWatchingForExternalChanges()
      {
         this.externalChangeWatcher?.Dispose();
         this.externalChangeWatcher = null;
         this.externalChangeDebounceTimer?.Dispose();
         this.externalChangeDebounceTimer = null;
      }

      private void RunOnOwnerThread(Action action)
      {
         if (this.ownerSyncContext != null)
         {
            this.ownerSyncContext.Post(_ => action(), null);
         }
         else
         {
            action();
         }
      }
   }
}
