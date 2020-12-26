mod crossover_settings;
mod mutation_settings;

pub use crossover_settings::*;
pub use mutation_settings::*;

use super::NeuralNetwork;
use crate::gen::SpecimenWriter;
use rand::prelude::*;
use std::mem;

pub fn crossover<R: Rng + ?Sized>(
    rng: &mut R,
    input: &[&NeuralNetwork],
    output: &mut SpecimenWriter<NeuralNetwork>,
    settings: &mut CrossoverSettings,
) {
    debug_assert!(input.len() == 2);

    let layers = input[0].layers().len();

    let mut a = input[0].clone();
    let mut b = input[1].clone();

    for _ in 0..settings.gen_nodes_affected(rng) {
        let layer = rng.gen_range(0, layers);
        let node = rng.gen_range(0, input[0].layers()[layer].activations().len());

        let a_weights = a.layers_mut()[layer].node_weights_mut(node).unwrap();
        let b_weights = b.layers_mut()[layer].node_weights_mut(node).unwrap();

        match settings.gen_method(rng) {
            CrossoverMethod::SwapWholeNode => {
                for (w_a, w_b) in a_weights.iter_mut().zip(b_weights) {
                    mem::swap(w_a, w_b);
                }
            }
            CrossoverMethod::SwapSomeWeights {
                min_weights_swapped_ratio,
                max_weights_swapped_ratio,
            } => {
                let node_weights = a_weights.len();
                let weights_to_swap = (node_weights as f64
                    * rng.gen_range(min_weights_swapped_ratio, max_weights_swapped_ratio))
                .trunc() as usize;

                (0..node_weights).choose_multiple_fill(
                    rng,
                    &mut settings.node_index_buffer()[..weights_to_swap],
                );

                for w_idx in settings.node_index_buffer()[..weights_to_swap]
                    .iter()
                    .copied()
                {
                    mem::swap(&mut a_weights[w_idx], &mut b_weights[w_idx]);
                }
            }
        }
    }

    output.write(a);
    output.write(b);
}

pub fn mutate<R: Rng + ?Sized>(
    rng: &mut R,
    nn: &mut NeuralNetwork,
    settings: &mut MutationSettings,
) {
    if rng.gen_range(0.0, 1.0) >= settings.mutation_probability() {
        return;
    }

    for _ in 0..settings.gen_weights_affected(rng) {
        let layer = rng.gen_range(0, nn.layers().len());
        let weight = nn.layers_mut()[layer]
            .all_weights_mut()
            .choose_mut(rng)
            .unwrap();

        match settings.gen_method(rng) {
            mutation_settings::MutationMethod::Invert => *weight = -*weight,
            mutation_settings::MutationMethod::Replace(min, max) => {
                *weight = rng.gen_range(min, max)
            }
            mutation_settings::MutationMethod::Scale(min, max) => {
                *weight *= rng.gen_range(min, max)
            }
            mutation_settings::MutationMethod::Shift(min, max) => {
                *weight += rng.gen_range(min, max)
            }
        }
    }
}
