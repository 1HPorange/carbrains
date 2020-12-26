- [ ] Proper attributes on all types (Copy, Clone etc.)
- [ ] Error reporting through ffi
- [ ] Safe FFI

pub struct Population {
    members: Vec<nn::NeuralNetwork>,
    config: Config,
}

*const c_void get_last_error()
<!-- build_config_from_json(json: *const i8, population: *mut *const c_void) -> BrainsError -->
build_population_from_config(path: *const i8, population: *mut *const Population) -> BrainsError
evolve_population(population: *mut *const Population, fitness: *const c_double) -> BrainsError
remove_population(population: *mut Population) -> BrainsError
save_top_n(population: *const Population, fitness: *const c_double, n: usize) -> BrainsError
save_all(population: *const Population) -> BrainsError