use crate::{error::BrainsError, nn::NeuralNetwork};
use rand::distributions::weighted::alias_method::WeightedIndex;
use rand::prelude::*;
use serde::{Deserialize, Serialize};

#[derive(Deserialize, Serialize, Clone)]
pub struct MutationSettingsTemplate {
    pub mutation_probability: f64,
    pub min_weights_affected_ratio: f64,
    pub max_weights_affected_ratio: f64,
    pub methods: Vec<MutationMethodProbability>,
}

impl Default for MutationSettingsTemplate {
    fn default() -> Self {
        MutationSettingsTemplate {
            mutation_probability: 0.3,
            min_weights_affected_ratio: 0.0,
            max_weights_affected_ratio: 0.2,
            methods: vec![
                MutationMethodProbability {
                    method: MutationMethod::Invert,
                    relative_probability: 0.5,
                },
                MutationMethodProbability {
                    method: MutationMethod::Replace(-2.0, 2.0),
                    relative_probability: 1.0,
                },
                MutationMethodProbability {
                    method: MutationMethod::Scale(-2.0, 2.0),
                    relative_probability: 4.0,
                },
                MutationMethodProbability {
                    method: MutationMethod::Shift(-2.0, 2.0),
                    relative_probability: 2.0,
                },
            ],
        }
    }
}

pub struct MutationSettings {
    mutation_probability: f64,
    min_weights_affected: usize,
    max_weights_affected: usize,
    methods: Vec<MutationMethod>,
    method_index: WeightedIndex<f64>,
}

#[derive(Deserialize, Serialize, Clone)]
pub struct MutationMethodProbability {
    method: MutationMethod,
    relative_probability: f64,
}

#[derive(Deserialize, Serialize, Clone, Copy)]
pub enum MutationMethod {
    Invert,
    Replace(f64, f64),
    Scale(f64, f64),
    Shift(f64, f64),
}

impl MutationSettings {
    pub fn new(
        template: &MutationSettingsTemplate,
        nn: &NeuralNetwork,
    ) -> Result<MutationSettings, BrainsError> {
        Self::validate_template(&template)?;

        let total_weights = nn
            .layers()
            .iter()
            .map(|l| l.all_weights().len())
            .sum::<usize>() as f64;

        let min_weights_affected =
            (template.min_weights_affected_ratio * total_weights).trunc() as usize;
        let max_weights_affected =
            (template.max_weights_affected_ratio * total_weights).trunc() as usize + 1;

        let methods = template.methods.iter().map(|m| m.method).collect();
        let method_index = WeightedIndex::new(
            template
                .methods
                .iter()
                .map(|mmp| mmp.relative_probability)
                .collect(),
        )
        .map_err(|_| BrainsError::MutationInvalidMethodProbabilities)?;

        Ok(MutationSettings {
            mutation_probability: template.mutation_probability,
            min_weights_affected,
            max_weights_affected,
            methods,
            method_index,
        })
    }

    pub fn mutation_probability(&self) -> f64 {
        self.mutation_probability
    }

    pub fn gen_weights_affected<R: Rng + ?Sized>(&self, rng: &mut R) -> usize {
        rng.gen_range(self.min_weights_affected, self.max_weights_affected)
    }

    pub fn gen_method<R: Rng + ?Sized>(&self, rng: &mut R) -> MutationMethod {
        self.methods[self.method_index.sample(rng)]
    }

    fn validate_template(template: &MutationSettingsTemplate) -> Result<(), BrainsError> {
        if template.mutation_probability < 0.0 || template.mutation_probability > 1.0 {
            return Err(BrainsError::MutationInvalidProbability);
        }

        if template.min_weights_affected_ratio < 0.0 || template.min_weights_affected_ratio > 1.0 {
            return Err(BrainsError::MutationInvalidMinWeightsAffectedRatio);
        }

        if template.max_weights_affected_ratio < template.min_weights_affected_ratio
            || template.max_weights_affected_ratio > 1.0
        {
            return Err(BrainsError::MutationInvalidMaxWeightsAffectedRatio);
        }

        if template.methods.is_empty() {
            return Err(BrainsError::MutationMethodsEmpty);
        }

        for mmp in &template.methods {
            match mmp.method {
                MutationMethod::Invert => {}
                MutationMethod::Replace(min, max) => {
                    if max < min {
                        return Err(BrainsError::MutationInvalidReplaceMethodMinMax);
                    }
                }
                MutationMethod::Scale(min, max) => {
                    if max < min {
                        return Err(BrainsError::MutationInvalidScaleMethodMinMax);
                    }
                }
                MutationMethod::Shift(min, max) => {
                    if max < min {
                        return Err(BrainsError::MutationInvalidShiftMethodMinMax);
                    }
                }
            }
        }

        Ok(())
    }
}
