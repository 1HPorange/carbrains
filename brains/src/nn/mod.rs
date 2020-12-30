pub mod gen;

use crate::error::BrainsError;
use serde::{Deserialize, Serialize};
use std::{
    cell::{Ref, RefCell},
    fmt::Debug,
    iter,
    ops::Deref,
};

#[derive(Serialize, Deserialize, Clone)]
pub struct NeuralNetworkTemplate {
    input_count: usize,
    layers: Vec<Vec<Activation>>,
}

impl Default for NeuralNetworkTemplate {
    fn default() -> Self {
        NeuralNetworkTemplate {
            input_count: 19,
            layers: vec![
                vec![
                    Activation::TanH,
                    Activation::TanH,
                    Activation::TanH,
                    Activation::TanH,
                    Activation::TanH,
                    Activation::TanH,
                    Activation::TanH,
                    Activation::TanH,
                    Activation::TanH,
                    Activation::TanH,
                ],
                vec![Activation::TanH, Activation::TanH],
            ],
        }
    }
}

#[derive(Clone, Debug, Serialize, Deserialize)]
pub struct NeuralNetwork {
    layers: RefCell<Vec<Layer>>,
}

impl NeuralNetwork {
    pub fn from_template(template: &NeuralNetworkTemplate) -> Result<NeuralNetwork, BrainsError> {
        if template.input_count == 0 {
            return Err(BrainsError::NeuralNetworkConfigInputCountNull);
        }

        if template.layers.is_empty() {
            return Err(BrainsError::NeuralNetworkConfigNoLayers);
        }

        if template.layers.iter().any(|l| l.is_empty()) {
            return Err(BrainsError::NeuralNetworkConfigEmptyLayer);
        }

        let mut template_layers = template.layers.iter().cloned();

        let mut layers = Vec::new();

        layers.push(Layer::new(
            template.input_count,
            template_layers.next().unwrap(),
        ));

        for activations in template_layers {
            layers.push(Layer::new(
                layers.last().unwrap().activations.len(),
                activations,
            ));
        }

        Ok(NeuralNetwork {
            layers: RefCell::new(layers),
        })
    }

    pub fn new(input_count: usize, mut activation_layers: Vec<Vec<Activation>>) -> NeuralNetwork {
        assert!(activation_layers.len() > 0);

        let mut activation_layers = activation_layers.drain(0..activation_layers.len());

        let mut layers = Vec::new();

        layers.push(Layer::new(input_count, activation_layers.next().unwrap()));

        for activations in activation_layers {
            layers.push(Layer::new(
                layers.last().unwrap().activations.len(),
                activations,
            ));
        }

        NeuralNetwork {
            layers: RefCell::new(layers),
        }
    }

    pub fn is_structurally_equal(&self, other: &NeuralNetwork) -> bool {
        self.layers
            .borrow()
            .iter()
            .zip(other.layers.borrow().iter())
            .all(|(s, o)| {
                s.activations.len() == o.activations.len()
                    && s.weights.len() == o.weights.len()
                    && s.input_count == o.input_count
            })
    }

    pub fn total_nodes(&self) -> usize {
        self.layers
            .borrow()
            .iter()
            .map(|l| l.activations.len())
            .sum()
    }

    pub fn layers<'a>(&'a self) -> impl Deref<Target = [Layer]> + 'a {
        Ref::map(self.layers.borrow(), |l| &l[..])
    }

    pub fn layers_mut<'a>(&mut self) -> &mut [Layer] {
        &mut self.layers.get_mut()[..]
    }

    pub fn input_count(&self) -> usize {
        self.layers.borrow()[0].input_count
    }

    pub fn output_count(&self) -> usize {
        self.layers.borrow().last().unwrap().output.len()
    }

    pub fn evaluate<'a>(&'a self, inputs: &[f64]) -> impl Deref<Target = [f64]> + 'a {
        {
            let mut layers = self.layers.borrow_mut();

            layers[0].evaluate(inputs);

            for idx in 1..layers.len() {
                let (prev, current) = layers[idx - 1..=idx].split_at_mut(1);
                current[0].evaluate(&prev[0].output);
            }
        }

        Ref::map(self.layers.borrow(), |l| &l.last().unwrap().output[..])
    }
}

#[derive(Clone, Debug, Deserialize, Serialize)]
pub struct Layer {
    input_count: usize,
    activations: Vec<Activation>,
    weights: Vec<f64>,
    activation_buffer: Vec<f64>,
    output: Vec<f64>,
}

impl Layer {
    fn new(input_count: usize, activations: Vec<Activation>) -> Layer {
        assert!(input_count > 0);
        assert!(activations.len() > 0);

        Layer {
            input_count,
            weights: vec![0.0; (input_count + 1) * activations.len()],
            activation_buffer: Vec::with_capacity(input_count + 1),
            output: vec![0.0; activations.len()],
            activations,
        }
    }

    pub fn activations(&self) -> &[Activation] {
        &self.activations[..]
    }

    pub fn activations_mut(&mut self) -> &mut [Activation] {
        &mut self.activations[..]
    }

    pub fn all_weights(&self) -> &[f64] {
        &self.weights[..]
    }

    pub fn all_weights_mut(&mut self) -> &mut [f64] {
        &mut self.weights[..]
    }

    pub fn node_weights(&self, node: usize) -> Option<&[f64]> {
        self.weights.chunks_exact(self.input_count + 1).nth(node)
    }

    pub fn node_weights_mut(&mut self, node: usize) -> Option<&mut [f64]> {
        self.weights
            .chunks_exact_mut(self.input_count + 1)
            .nth(node)
    }

    fn evaluate(&mut self, input: &[f64]) -> &[f64] {
        assert_eq!(input.len(), self.input_count);

        for (idx, node_weights) in self.weights.chunks_exact(self.input_count + 1).enumerate() {
            self.activation_buffer.clear();
            self.activation_buffer.extend(
                iter::once(1.0)
                    .chain(input.iter().copied())
                    .zip(node_weights)
                    .map(|(i, w)| i * w),
            );

            self.output[idx] = self.activations[idx].evaluate(&self.activation_buffer);
        }

        &self.output
    }
}

#[derive(Clone, Copy, Serialize, Deserialize)]
pub enum Activation {
    Linear,
    TanH,
    #[serde(skip)]
    Custom(fn(&[f64]) -> f64),
}

impl Activation {
    pub fn evaluate(&self, input: &[f64]) -> f64 {
        match self {
            Activation::Linear => input.iter().sum(),
            Activation::TanH => input.iter().sum::<f64>().tanh(),
            Activation::Custom(f) => f(input),
        }
    }
}

impl Debug for Activation {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Activation::Linear => f.write_str("Linear"),
            Activation::TanH => f.write_str("TanH"),
            Activation::Custom(_) => f.write_str("Custom"),
        }
    }
}
