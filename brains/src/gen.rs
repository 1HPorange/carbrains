use std::cmp::Ordering;

use rand::{distributions::weighted::alias_method::WeightedIndex, prelude::*};
use serde::{Deserialize, Serialize};

pub struct SpecimenWriter<'a, S> {
    limit: usize,
    buffer: &'a mut Vec<S>,
}

impl<'a, S> SpecimenWriter<'a, S> {
    pub fn write(&mut self, specimen: S) {
        if self.can_write() {
            self.buffer.push(specimen);
        }
    }

    pub fn can_write(&self) -> bool {
        self.buffer.len() < self.limit
    }
}

pub fn evolve<R, S, C, CS, M, MS>(
    rng: &mut R,
    population: &[S],
    fitness: &[f64],
    selection_method: SelectionMethod,
    elitism: usize,
    crossover_inputs: usize,
    crossover: C,
    crossover_settings: &CS,
    mutate: M,
    mutate_settings: &MS,
) -> Vec<S>
where
    S: Clone,
    R: Rng + ?Sized,
    C: Fn(&mut R, &[&S], &mut SpecimenWriter<S>, &CS, &mut Vec<usize>),
    M: Fn(&mut R, &mut S, &MS),
{
    assert_eq!(population.len(), fitness.len());

    let mut output = Vec::with_capacity(population.len());

    let mut output_writer = SpecimenWriter {
        limit: population.len(),
        buffer: &mut output,
    };

    add_elitism_members(&mut output_writer, population, fitness, elitism);

    let mut crossover_input_buffer = Vec::with_capacity(crossover_inputs);
    let mut crossover_weight_index_buffer = Vec::new();

    // TODO: Proper error handling
    let fitness_alias_table = WeightedIndex::new(Vec::from(fitness)).unwrap();

    while output_writer.can_write() {
        crossover_input_buffer.clear();

        // selection
        selection(
            rng,
            selection_method,
            population,
            &fitness_alias_table,
            crossover_inputs,
            &mut crossover_input_buffer,
        );

        // crossover
        crossover(
            rng,
            &crossover_input_buffer,
            &mut output_writer,
            crossover_settings,
            &mut crossover_weight_index_buffer,
        );

        crossover_weight_index_buffer.clear();
    }

    // mutation
    output
        .iter_mut()
        .skip(elitism)
        .for_each(|s| mutate(rng, s, mutate_settings));

    output
}

// TODO: Think about if this really belongs into .evolve()
// TODO: Attempt to re-use this for save_top_n
fn add_elitism_members<S: Clone>(
    output_writer: &mut SpecimenWriter<S>,
    population: &[S],
    fitness: &[f64],
    elitism: usize,
) {
    if elitism == 0 {
        return;
    }

    // TODO: Think of a more space-efficient implementation
    let mut elite = population.iter().zip(fitness).collect::<Vec<_>>();
    elite.sort_unstable_by(|a, b| b.1.partial_cmp(&a.1).unwrap_or(Ordering::Equal));

    for &(specimen, _) in elite.iter().take(elitism) {
        output_writer.write(specimen.clone());
    }
}

#[derive(Copy, Clone, Deserialize, Serialize)]
pub enum SelectionMethod {
    /// Sum fitness, divive by sum, chose t in [0-1], take first element at accumulate sum >= t
    FitnessProportionate,

    /// Equally spaced indices, randomly generated once
    StochasticUniversal,

    /// Randomly chose subset, take strongest
    Tournament(usize),

    /// Take only the best X percent
    Truncation(f64),
}

impl Default for SelectionMethod {
    fn default() -> Self {
        SelectionMethod::FitnessProportionate
    }
}

fn selection<'a, R: Rng + ?Sized, S>(
    rng: &mut R,
    method: SelectionMethod,
    population: &'a [S],
    fitness_alias_table: &WeightedIndex<f64>,
    count: usize,
    output: &mut Vec<&'a S>,
) {
    match method {
        SelectionMethod::FitnessProportionate => {
            for _ in 0..count {
                let candidate_idx = fitness_alias_table.sample(rng);
                output.push(&population[candidate_idx]);
            }
        }
        _ => unimplemented!(),
    }
}
