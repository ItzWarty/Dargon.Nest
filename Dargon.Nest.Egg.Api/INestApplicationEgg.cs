﻿namespace Dargon.Nest.Egg {
   public interface INestApplicationEgg {
      NestResult Start(IEggParameters parameters);
      NestResult Shutdown(ShutdownReason reason);
   }
}
