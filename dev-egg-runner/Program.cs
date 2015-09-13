﻿using Castle.DynamicProxy;
using CommandLine;
using Dargon.Nest.Eggxecutor;
using Dargon.PortableObjects;
using Dargon.PortableObjects.Streams;
using Dargon.Services;
using Dargon.Services.Messaging;
using ItzWarty;
using ItzWarty.Collections;
using ItzWarty.IO;
using ItzWarty.Networking;
using ItzWarty.Threading;
using System;
using System.IO;

namespace dev_egg_runner {
   public class Options {
      [Option('e', "egg", DefaultValue = "dev-egg-example",
              HelpText = "Name of the Dargon Egg to run.")]
      public string EggName { get; set; }

      [Option('n', "name", DefaultValue = null,
              HelpText = "Name of the instance to run.")]
      public string InstanceName { get; set; }

      [Option('p', "port", DefaultValue = 21337,
              HelpText = "Port of the nest cluster.")]
      public int NestPort { get; set; }
   }

   public static class Program {
      public const int kHeartbeatIntervalMilliseconds = 1000;

      public static void Main(string[] args) {
         var options = new Options();
         if (!Parser.Default.ParseArguments(args, options)) {
            return;
         }

         IStreamFactory streamFactory = new StreamFactory();
         ICollectionFactory collectionFactory = new CollectionFactory();
         IThreadingFactory threadingFactory = new ThreadingFactory();
         ISynchronizationFactory synchronizationFactory = new SynchronizationFactory();
         IThreadingProxy threadingProxy = new ThreadingProxy(threadingFactory, synchronizationFactory);
         IDnsProxy dnsProxy = new DnsProxy();
         ITcpEndPointFactory tcpEndPointFactory = new TcpEndPointFactory(dnsProxy);
         INetworkingInternalFactory networkingInternalFactory = new NetworkingInternalFactory(threadingProxy, streamFactory);
         ISocketFactory socketFactory = new SocketFactory(tcpEndPointFactory, networkingInternalFactory);
         INetworkingProxy networkingProxy = new NetworkingProxy(socketFactory, tcpEndPointFactory);

         IPofContext pofContext = new PofContext().With(x => {
            x.MergeContext(new DspPofContext());               // 0 - 999
            x.MergeContext(new ExeggutorPofContext(3000));     // 3000 - 3499, must reflect value in nestd
         });
         IPofSerializer pofSerializer = new PofSerializer(pofContext);

         ProxyGenerator proxyGenerator = new ProxyGenerator();
         PofStreamsFactory pofStreamsFactory = new PofStreamsFactoryImpl(threadingProxy, streamFactory, pofSerializer);
         IServiceClientFactory serviceClientFactory = new ServiceClientFactory(proxyGenerator, streamFactory, collectionFactory, threadingProxy, networkingProxy, pofSerializer, pofStreamsFactory);
         var client = serviceClientFactory.CreateOrJoin(new ClusteringConfiguration(options.NestPort, kHeartbeatIntervalMilliseconds, ClusteringRoleFlags.GuestOnly));
         var exeggutor = client.GetService<ExeggutorService>();
         var ms = new MemoryStream();
         pofSerializer.Serialize(ms, (object)"test");
         try {
            exeggutor.SpawnHatchling(options.EggName, new SpawnConfiguration {
               Arguments = ms.ToArray(),
               InstanceName = options.InstanceName
            });
         } catch (PortableException e) {
            Console.WriteLine(e.InnerException);
         }
      }
   }
}
