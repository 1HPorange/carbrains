use crate::error::BrainsError;

use super::*;
use rand::{distributions::weighted::alias_method::WeightedIndex, prelude::*};
use serde::{Deserialize, Serialize};

#[derive(Deserialize, Serialize, Clone)]
pub struct CrossoverSettingsTemplate {
    pub min_nodes_affected_ratio: f64,
    pub max_nodes_affected_ratio: f64,
    pub methods: Vec<CrossoverMethodProbability>,
}

impl Default for CrossoverSettingsTemplate {
    fn default() -> Self {
        CrossoverSettingsTemplate {
            min_nodes_affected_ratio: 0.02,
            max_nodes_affected_ratio: 0.3,
            methods: vec![
                CrossoverMethodProbability {
                    method: CrossoverMethod::SwapWholeNode,
                    relative_probability: 3.0,
                },
                CrossoverMethodProbability {
                    method: CrossoverMethod::SwapSomeWeights {
                        min_weights_swapped_ratio: 0.25,
                        max_weights_swapped_ratio: 0.75,
                    },
                    relative_probability: 1.0,
                },
            ],
        }
    }
}

#[derive(Deserialize, Serialize, Clone)]
pub struct CrossoverMethodProbability {
    pub method: CrossoverMethod,
    pub relative_probability: f64,
}

pub struct CrossoverSettings {
    min_nodes_affected: usize,
    max_nodes_affected: usize,
    node_index_buffer: Box<[usize]>,
    methods: Vec<CrossoverMethod>,
    method_index: WeightedIndex<f64>,
}

#[derive(Deserialize, Serialize, Clone, Copy)]
pub enum CrossoverMethod {
    SwapWholeNode,
    SwapSomeWeights {
        min_weights_swapped_ratio: f64,
        max_weights_swapped_ratio: f64,
    },
}

impl CrossoverSettings {
    pub fn new(
        template: &CrossoverSettingsTemplate,
        nn: &NeuralNetwork,
    ) -> Result<CrossoverSettings, BrainsError> {
        Self::validate_template(&template)?;

        let total_nodes = nn.total_nodes() as f64;

        let min_nodes_affected = (template.min_nodes_affected_ratio * total_nodes).trunc() as usize;
        let max_nodes_affected =
            (template.max_nodes_affected_ratio * total_nodes).trunc() as usize + 1;

        let methods = template.methods.iter().map(|m| m.method).collect();
        let method_index = WeightedIndex::new(
            template
                .methods
                .iter()
                .map(|cmp| cmp.relative_probability)
                .collect(),
        )
        .map_err(|_| BrainsError::CrossoverInvalidMethodProbabilities)?;

        // TODO: Make this work for non-uniform node weight counts across a layer
        let max_node_weights = nn
            .layers()
            .iter()
            .max_by_key(|l| l.node_weights(0).unwrap().len())
            .unwrap()
            .node_weights(0)
            .unwrap()
            .len();
        let node_index_buffer = (0..max_node_weights).collect::<Box<[usize]>>();

        Ok(CrossoverSettings {
            min_nodes_affected,
            max_nodes_affected,
            node_index_buffer,
            methods,
            method_index,
        })
    }

    pub fn gen_nodes_affected<R: Rng + ?Sized>(&self, rng: &mut R) -> usize {
        rng.gen_range(self.min_nodes_affected, self.max_nodes_affected)
    }

    pub fn gen_method<R: Rng + ?Sized>(&self, rng: &mut R) -> CrossoverMethod {
        self.methods[self.method_index.sample(rng)]
    }

    pub fn node_index_buffer(&mut self) -> &mut [usize] {
        &mut self.node_index_buffer[..]
    }

    fn validate_template(template: &CrossoverSettingsTemplate) -> Result<(), BrainsError> {
        if template.min_nodes_affected_ratio < 0.0 || template.min_nodes_affected_ratio > 1.0 {
            return Err(BrainsError::CrossoverInvalidMinNodeRatio);
        }

        if template.max_nodes_affected_ratio < template.min_nodes_affected_ratio
            || template.max_nodes_affected_ratio > 1.0
        {
            return Err(BrainsError::CrossoverInvalidMaxNodeRatio);
        }

        if template.methods.is_empty() {
            return Err(BrainsError::CrossoverEmptyMethodProbabilities);
        }

        for cmp in &template.methods {
            match cmp.method {
                CrossoverMethod::SwapWholeNode => {}
                CrossoverMethod::SwapSomeWeights {
                    min_weights_swapped_ratio,
                    max_weights_swapped_ratio,
                } => {
                    if min_weights_swapped_ratio < 0.0
                        || min_weights_swapped_ratio > 1.0
                        || max_weights_swapped_ratio < min_weights_swapped_ratio
                        || max_weights_swapped_ratio > 1.0
                    {
                        return Err(BrainsError::CrossoverInvalidSwapWeightsRatios);
                    }
                }
            }
        }

        Ok(())
    }
}
