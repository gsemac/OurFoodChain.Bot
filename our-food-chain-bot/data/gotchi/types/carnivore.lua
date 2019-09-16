function register(type)

	type.SetName("carnivore")
	type.SetColor(255, 0, 0)

	type.SetMatchup("carnivore", 1.5)
	type.SetMatchup("autotroph", 0.5)

	type.Requires.RoleMatch("predator|carnivore")

end