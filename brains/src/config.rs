use serde::{Deserialize, Serialize};

use crate::{
    error::BrainsError,
    gen::SelectionMethod,
    nn::{
        gen::{
            CrossoverSettings, CrossoverSettingsTemplate, MutationSettings,
            MutationSettingsTemplate,
        },
        NeuralNetwork, NeuralNetworkTemplate,
    },
};

#[derive(Deserialize, Serialize, Clone)]
pub struct ConfigTemplate {
    pub population_size: usize,
    pub min_weight: f64,
    pub max_weight: f64,
    pub elitism: f64,
    pub network: NeuralNetworkTemplate,
    pub selection_method: SelectionMethod,
    pub crossover: CrossoverSettingsTemplate,
    pub mutation: MutationSettingsTemplate,
}

impl Default for ConfigTemplate {
    fn default() -> Self {
        ConfigTemplate {
            population_size: 500,
            min_weight: -1.0,
            max_weight: 1.0,
            elitism: 0.02,
            network: Default::default(),
            selection_method: Default::default(),
            crossover: Default::default(),
            mutation: Default::default(),
        }
    }
}

pub struct Config {
    elitism: usize,
    crossover: CrossoverSettings,
    mutation: MutationSettings,
    selection_method: SelectionMethod,
    network: NeuralNetwork,
    template: ConfigTemplate,
}

impl Config {
    pub fn build_from_template(template: &ConfigTemplate) -> Result<Config, BrainsError> {
        if template.population_size == 0 {
            return Err(BrainsError::PopulationSizeZero);
        }

        if template.max_weight < template.min_weight {
            return Err(BrainsError::MinWeightLargerThanMaxWeight);
        }

        if template.elitism < 0.0 || template.elitism > 1.0 {
            return Err(BrainsError::InvalidElitismRatio);
        }

        let elitism = (template.elitism * template.population_size as f64).trunc() as usize;

        let network = match NeuralNetwork::from_template(&template.network) {
            Ok(network) => network,
            Err(e) => return Err(e),
        };

        let crossover = match CrossoverSettings::new(&template.crossover, &network) {
            Ok(crossover) => crossover,
            Err(e) => return Err(e),
        };

        let mutation = match MutationSettings::new(&template.mutation, &network) {
            Ok(mutation) => mutation,
            Err(e) => return Err(e),
        };

        Ok(Config {
            elitism,
            selection_method: template.selection_method,
            crossover,
            mutation,
            network,
            template: template.clone(),
        })
    }

    pub fn elitism(&self) -> usize {
        self.elitism
    }

    pub fn selection_method(&self) -> SelectionMethod {
        self.selection_method
    }

    pub fn crossover_settings(&self) -> &CrossoverSettings {
        &self.crossover
    }

    pub fn mutation_settings(&self) -> &MutationSettings {
        &self.mutation
    }

    pub fn network(&self) -> &NeuralNetwork {
        &self.network
    }

    pub fn template(&self) -> &ConfigTemplate {
        &self.template
    }
}
