﻿using Dargon.Nest.Egg;
using Dargon.Nest.Exeggutor.Host.PortableObjects;
using ItzWarty;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace nest_host {
   public class EggHost : IEggHost {
      private readonly CountdownEvent shutdownLatch = new CountdownEvent(1);

      public void Run(BootstrapDto bootstrapArguments) {
         Console.WriteLine(bootstrapArguments.EggPath);
         Console.WriteLine(bootstrapArguments.Name);
         Console.WriteLine(bootstrapArguments.PayloadBytes.Length);

         var eggStartPath = Path.Combine(bootstrapArguments.EggPath, "nest-main.dll");
         if (!File.Exists(eggStartPath)) {
            // todo
         }

         var eggStartAssembly = Assembly.LoadFile(eggStartPath);
         var candidates = eggStartAssembly.GetExportedTypes().Where(FilterNestApplicationEggs).ToArray();
         if (candidates.Length != 1) {
            Console.Error.WriteLine("Too many candidates! Found " + candidates.Select(c => c.FullName).Join(", "));
         } else {
            var eggClass = candidates.First();
            var eggInstance = (INestApplicationEgg)Activator.CreateInstance(eggClass);
            var startResult = eggInstance.Start(new EggParameters(this, bootstrapArguments.Name, bootstrapArguments.PayloadBytes));
            Console.WriteLine("Egg started with " + startResult);
         }

         shutdownLatch.Wait();
      }

      public void Shutdown() {
         shutdownLatch.Signal();
      }

      private bool FilterNestApplicationEggs(Type type) {
         return type.GetInterfaces().Any(i => i.Name.Contains("INestApplicationEgg"));
      }
   }
}