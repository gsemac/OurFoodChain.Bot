function register(type)

	type.SetName("herbivore")
	type.SetColor(0, 0, 255)

	type.SetMatchup("autotroph", 1.5)

	type.Requires.RoleMatch("base-consumer|herbivore")

end