using System.Collections.Generic;
using Entitas;
using Sample.MultiAssembly.FeatureB;

namespace Sample.MultiAssembly.FeatureA
{
    public sealed class SharedReactiveSystem : ReactiveSystem<SharedEntity>
    {
        public SharedReactiveSystem(SharedContext context) : base(context)
        {
        }

        protected override ICollector<SharedEntity> GetTrigger(IContext<SharedEntity> context)
        {
            return context.CreateCollector(SharedPlayerMatcher.Level().Added());
        }

        protected override bool Filter(SharedEntity entity)
        {
            return entity.HasPlayer();
        }

        protected override void Execute(List<SharedEntity> entities)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                var playerHealth = entity.HasHealth() ? entity.GetHealth().Value : 0;
                var resultHealth = playerHealth + 10;
                entity.ReplaceRoot(13f);
                entity.ReplaceHealth(resultHealth);
            }
        }
    }
}
