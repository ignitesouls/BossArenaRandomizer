using System;

namespace BossArenaRandomizer.Services
{
    public sealed class SeedGenerationService
    {
        private readonly GenerationService _generationService;

        public SeedGenerationService()
            : this(new GenerationService())
        {
        }

        public SeedGenerationService(GenerationService generationService)
        {
            _generationService = generationService ?? throw new ArgumentNullException(nameof(generationService));
        }

        public GenerationResult Generate(GenerationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _generationService.Generate(request);
        }
    }
}
