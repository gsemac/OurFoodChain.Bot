function onRegister(type)

	-- Good HP with low attack and speed.

	type.SetName("decomposer")
	type.SetColor(139, 69, 19)

	type.SetBaseHp(50)
	type.SetBaseAtk(40)
	type.SetBaseDef(49)
	type.SetBaseSpd(40)

	type.SetMatchup("carnivore", 1.2)
	type.SetMatchup("autotroph", 1.5)

	type.Requires.RoleMatch("scavenger|detritivore|decomposer|fungus")

end