function onRegister(type)

	-- Good HP with low attack and speed.

	type.SetName("decomposer")
	type.SetAliasPattern("scavenger|detritivore|decomposer|fungus")
	type.SetColor(139, 69, 19)

	type.SetBaseHp(50)
	type.SetBaseAtk(40)
	type.SetBaseDef(49)
	type.SetBaseSpd(40)

	type.SetMatchup("predator", 1.2)
	type.SetMatchup("producer", 1.5)

	type.Requires.RoleMatch(type.AliasPattern)

end