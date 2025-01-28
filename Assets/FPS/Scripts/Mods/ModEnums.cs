/* -- General Enums -- */

// damage elements
public enum Elements {
    normal,
    fire,
    ice,
    acid,
    lightning,
    necrotic,
    radiant
}

// possible rarity of items
public enum Rarity {
    none,
    rare,
    mediumRare,
    medium,
    mediumWell,
    wellDone
}

/* -- Mod Enums -- */

// types that a modGroup (and mods) can be
public enum ModGroupTypes {
    barrel,
    grip,
    stock,
    optic,
    magazine
}

// Types of effects a mod can have
public enum ModType {
    damageChange,
    statChange,
    onFunction,
    onTimer
}

// onFunction functions
public enum FunctionType {
    weaponUpdate,
    weaponFire,
    weaponReload,
    bulletUpdate,
    bulletImpact
}

// All stats a weapon has
public enum StatType {
    critChance,
    reloadTime,
    bulletsPerShot,
    fireDelay,
    spread,
    bulletVelocity,
    bulletAcceleration,
    magCapacity,
    ammoCapacity
}
