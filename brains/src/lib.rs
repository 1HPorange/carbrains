pub mod config;
pub mod error;
pub mod gen;
pub mod nn;

use std::{
    cmp::Ordering,
    ffi::{CStr, CString},
    fmt::Debug,
    fs,
    ptr::{self, NonNull},
    slice,
};

use config::{Config, ConfigTemplate};
use error::BrainsError;
use libc::{c_char, c_double};
use nn::NeuralNetwork;
use rand::prelude::*;

pub struct Population {
    members: Vec<nn::NeuralNetwork>,
    config: Option<Config>,
}

static mut LAST_ERROR: Option<CString> = None;

#[must_use]
fn with_last_error_extended<E: Debug>(error: BrainsError, description: E) -> BrainsError {
    unsafe {
        LAST_ERROR = CString::new(format!("{:?}: {:?}", error, description)).ok();
        error
    }
}

fn with_last_error(error: BrainsError) -> BrainsError {
    unsafe {
        LAST_ERROR = CString::new(format!("{:?}", error)).ok();
        error
    }
}

#[no_mangle]
pub unsafe extern "C" fn get_last_error() -> *const c_char {
    match &LAST_ERROR {
        Some(s) => s.as_ptr(),
        None => ptr::null(),
    }
}

// TODO: Represent the entire configuration as repr-C structs and pass it out here instead of having
// tons of annoying out-parameters
// TODO: Catch unwind pretty much everything in here
#[no_mangle]
pub unsafe extern "C" fn build_population_from_config(
    path: Option<NonNull<c_char>>,
    population: *mut *mut Population,
    count: *mut usize,
    inputs: *mut usize,
    outputs: *mut usize,
) -> BrainsError {
    let path = match path {
        Some(p) => match CStr::from_ptr(p.as_ptr()).to_str() {
            Ok(p) => p,
            Err(e) => {
                return with_last_error_extended(BrainsError::InvalidConfigPath, e);
            }
        },
        None => return with_last_error(BrainsError::ConfigPathNull),
    };

    if population.is_null() {
        return with_last_error(BrainsError::PopulationPointerNull);
    }

    if !(*population).is_null() {
        return with_last_error(BrainsError::PopulationPointerAlreadyInitialized);
    }

    let config_json = match fs::read_to_string(path) {
        Ok(json) => json,
        Err(_) => return with_last_error(BrainsError::CannotReadConfigFile),
    };

    let config_template: ConfigTemplate = match serde_json::from_str(&config_json) {
        Ok(config) => config,
        Err(e) => {
            return with_last_error_extended(BrainsError::InvalidConfigJson, e);
        }
    };

    let config = match Config::build_from_template(&config_template) {
        Ok(c) => c,
        Err(e) => return with_last_error(e),
    };

    let mut members = vec![config.network().clone(); config_template.population_size];

    // Shuffle network weight according to config
    let mut rng = thread_rng();

    for nn in &mut members {
        for nn_layer in nn.layers_mut() {
            for nn_weight in nn_layer.all_weights_mut() {
                *nn_weight = rng.gen_range(config_template.min_weight, config_template.max_weight);
            }
        }
    }

    *count = members.len();
    *inputs = members[0].input_count();
    *outputs = members[0].output_count();

    let population_box = Box::new(Population {
        members,
        config: Some(config),
    });
    *population = Box::into_raw(population_box);

    BrainsError::None
}

// TODO: Change this embarrasing function name
#[no_mangle]
pub unsafe extern "C" fn load_existing_population(
    members_path: Option<NonNull<c_char>>,
    config_path: Option<NonNull<c_char>>,
    population: *mut *mut Population,
    count: *mut usize,
    inputs: *mut usize,
    outputs: *mut usize,
) -> BrainsError {
    let members_path = match members_path {
        Some(p) => match CStr::from_ptr(p.as_ptr()).to_str() {
            Ok(p) => p,
            Err(e) => return with_last_error_extended(BrainsError::PopulationPathInvalid, e),
        },
        None => return with_last_error(BrainsError::PopulationPathNull),
    };

    let members_json = match fs::read_to_string(members_path) {
        Ok(j) => j,
        Err(e) => return with_last_error_extended(BrainsError::CannotReadMembersFile, e),
    };

    let members: Vec<NeuralNetwork> = match serde_json::from_str(&members_json) {
        Ok(m) => m,
        Err(e) => return with_last_error_extended(BrainsError::InvalidMembersJson, e),
    };

    if members.is_empty() {
        return with_last_error(BrainsError::PopulationSizeZero);
    }

    if !members
        .iter()
        .all(|nn| nn.input_count() == members[0].input_count())
    {
        return with_last_error(BrainsError::InconsistentNetworkInputCounts);
    }

    if !members
        .iter()
        .all(|nn| nn.output_count() == members[0].output_count())
    {
        return with_last_error(BrainsError::InconsistentNetworkOutputCounts);
    }

    let config = match config_path {
        Some(config_path) => {
            let config_path = match CStr::from_ptr(config_path.as_ptr()).to_str() {
                Ok(p) => p,
                Err(e) => return with_last_error_extended(BrainsError::InvalidConfigPath, e),
            };

            let config_json = match fs::read_to_string(config_path) {
                Ok(json) => json,
                Err(_) => return with_last_error(BrainsError::CannotReadConfigFile),
            };

            let config_template: ConfigTemplate = match serde_json::from_str(&config_json) {
                Ok(c) => c,
                Err(e) => return with_last_error_extended(BrainsError::InvalidConfigJson, e),
            };

            match Config::build_from_template(&config_template) {
                Ok(c) => Some(c),
                Err(e) => return with_last_error(e),
            }
        }
        None => None,
    };

    if !config
        .as_ref()
        .map(|c| c.network().is_structurally_equal(&members[0]))
        .unwrap_or(true)
    {
        return with_last_error(BrainsError::PopulationConfigMismatch);
    }

    *count = members.len();
    *inputs = members[0].input_count();
    *outputs = members[0].output_count();

    let population_box = Box::new(Population { members, config });
    *population = Box::into_raw(population_box);

    BrainsError::None
}

#[no_mangle]
pub unsafe extern "C" fn evaluate_member(
    population: Option<&Population>,
    index: usize,
    inputs: Option<NonNull<c_double>>,
    outputs: Option<NonNull<c_double>>,
) -> BrainsError {
    let population = match population {
        Some(p) => p,
        None => return with_last_error(BrainsError::PopulationPointerNull),
    };

    let network = match population.members.get(index) {
        Some(n) => n,
        None => return with_last_error(BrainsError::InvalidMemberIndex),
    };

    let inputs = match inputs {
        Some(i) => slice::from_raw_parts(i.as_ptr(), network.input_count()),
        None => return with_last_error(BrainsError::InputsPointerNull),
    };

    let outputs = match outputs {
        Some(o) => slice::from_raw_parts_mut(o.as_ptr(), network.output_count()),
        None => return with_last_error(BrainsError::OutputsPointerNull),
    };

    outputs.copy_from_slice(&*network.evaluate(inputs));

    BrainsError::None
}

#[no_mangle]
pub unsafe extern "C" fn evolve_population(
    population: Option<&mut Population>,
    fitness: Option<NonNull<c_double>>,
) -> BrainsError {
    let population = match population {
        Some(x) => x,
        None => return with_last_error(BrainsError::PopulationPointerNull),
    };
    let fitness = match fitness {
        Some(f) => slice::from_raw_parts(f.as_ptr(), population.members.len()),
        None => return with_last_error(BrainsError::FitnessPointerNull),
    };
    let config = match &population.config {
        Some(c) => c,
        None => return with_last_error(BrainsError::MissingEvolutionConfig),
    };

    let mut rng = thread_rng();

    let next_gen = gen::evolve(
        &mut rng,
        &population.members,
        fitness,
        config.selection_method(),
        config.elitism(),
        2,
        nn::gen::crossover,
        config.crossover_settings(),
        nn::gen::mutate,
        config.mutation_settings(),
    );

    // Also drops the old vector
    population.members = next_gen;

    BrainsError::None
}

#[no_mangle]
pub unsafe extern "C" fn drop_population(population: Option<NonNull<Population>>) -> BrainsError {
    match population {
        Some(p) => {
            drop(Box::from_raw(p.as_ptr()));
            BrainsError::None
        }
        None => with_last_error(BrainsError::PopulationPointerNull),
    }
}

#[no_mangle]
pub unsafe extern "C" fn save_top_n(
    path: Option<NonNull<c_char>>,
    population: Option<&Population>,
    fitness: Option<NonNull<c_double>>,
    n: usize,
) -> BrainsError {
    let path = match path {
        Some(p) => match CStr::from_ptr(p.as_ptr()).to_str() {
            Ok(p) => p,
            Err(e) => {
                return with_last_error_extended(BrainsError::InvalidOutputPath, e);
            }
        },
        None => return with_last_error(BrainsError::InvalidOutputPath),
    };

    let population = match population {
        Some(p) => p,
        None => return with_last_error(BrainsError::PopulationPointerNull),
    };

    let fitness = match fitness {
        Some(f) => slice::from_raw_parts(f.as_ptr(), population.members.len()),
        None => return with_last_error(BrainsError::FitnessPointerNull),
    };

    if n == 0 {
        return with_last_error(BrainsError::ExportMemberCountZero);
    }

    let n = n.min(population.members.len());

    let mut members = population
        .members
        .iter()
        .cloned()
        .zip(fitness.iter().copied())
        .collect::<Vec<_>>();

    members.sort_unstable_by(|a, b| b.1.partial_cmp(&a.1).unwrap_or(Ordering::Equal));

    let json = match serde_json::to_string_pretty(
        &members.iter().take(n).map(|m| &m.0).collect::<Vec<_>>(),
    ) {
        Ok(j) => j,
        Err(e) => {
            return with_last_error_extended(BrainsError::InternalError, e);
        }
    };

    match fs::write(path, json) {
        Ok(_) => BrainsError::None,
        Err(e) => {
            return with_last_error_extended(BrainsError::FileSaveError, e);
        }
    }
}

#[no_mangle]
pub unsafe extern "C" fn save_all(
    path: Option<NonNull<c_char>>,
    population: Option<&Population>,
) -> BrainsError {
    let path = match path {
        Some(p) => match CStr::from_ptr(p.as_ptr()).to_str() {
            Ok(p) => p,
            Err(e) => {
                return with_last_error_extended(BrainsError::InvalidOutputPath, e);
            }
        },
        None => return with_last_error(BrainsError::OutputPathNull),
    };

    let population = match population {
        Some(p) => p,
        None => return with_last_error(BrainsError::PopulationPointerNull),
    };

    let json = match serde_json::to_string_pretty(&population.members) {
        Ok(j) => j,
        Err(e) => {
            return with_last_error_extended(BrainsError::InternalError, e);
        }
    };

    match fs::write(path, json) {
        Ok(_) => BrainsError::None,
        Err(e) => {
            return with_last_error_extended(BrainsError::FileSaveError, e);
        }
    }
}

#[no_mangle]
pub unsafe extern "C" fn export_default_config(path: Option<NonNull<c_char>>) -> BrainsError {
    let path = match path {
        Some(p) => match CStr::from_ptr(p.as_ptr()).to_str() {
            Ok(p) => p,
            Err(e) => {
                return with_last_error_extended(BrainsError::InvalidOutputPath, e);
            }
        },
        None => return with_last_error(BrainsError::OutputPathNull),
    };

    let json = match serde_json::to_string_pretty(&ConfigTemplate::default()) {
        Ok(j) => j,
        Err(e) => return with_last_error_extended(BrainsError::InternalError, e),
    };

    match fs::write(path, json) {
        Ok(_) => BrainsError::None,
        Err(e) => with_last_error_extended(BrainsError::FileSaveError, e),
    }
}
