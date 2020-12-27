#[repr(u16)]
#[derive(Debug)]
pub enum BrainsError {
    None = 0,
    InternalError = 1,

    // Population
    PopulationPointerNull = 100,
    PopulationPointerAlreadyInitialized,

    // Config
    InvalidConfigPath = 200,
    CannotReadConfigFile,
    InvalidConfigJson,
    PopulationSizeZero,
    MinWeightLargerThanMaxWeight,
    ConfigPathNull,
    InvalidElitismRatio,

    // Neural network config
    NeuralNetworkConfigNoLayers = 300,
    NeuralNetworkConfigEmptyLayer,
    NeuralNetworkConfigInputCountNull,

    // Crossover config
    CrossoverInvalidMinNodeRatio = 500,
    CrossoverInvalidMaxNodeRatio,
    CrossoverInvalidMethodProbabilities,
    CrossoverEmptyMethodProbabilities,
    CrossoverInvalidSwapWeightsRatios,

    // Mutation config
    MutationInvalidProbability = 600,
    MutationInvalidMinWeightsAffectedRatio,
    MutationInvalidMaxWeightsAffectedRatio,
    MutationMethodsEmpty,
    MutationInvalidMethodProbabilities,
    MutationInvalidReplaceMethodMinMax,
    MutationInvalidScaleMethodMinMax,
    MutationInvalidShiftMethodMinMax,

    // Evolution
    FitnessPointerNull = 700,
    MissingEvolutionConfig,

    // Export
    InvalidOutputPath = 800,
    ExportMemberCountZero,
    FileSaveError,
    OutputPathNull,

    // Import
    PopulationPathNull = 900,
    PopulationPathInvalid,
    CannotReadMembersFile,
    InvalidMembersJson,

    // Evaluate
    InvalidMemberIndex = 1000,
    InputsPointerNull,
    OutputsPointerNull,
}
