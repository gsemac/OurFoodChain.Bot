function onRegister(type)

	-- Carnivores are fast and offensive.

	type.SetName("predator")
	type.SetAliasPattern("predator|carnivore")
	type.SetColor(255, 0, 0)
	
	type.SetBaseHp(39)
	type.SetBaseAtk(52)
	type.SetBaseDef(43)
	type.SetBaseSpd(65)

	type.SetMatchup("carnivore", 1.5)
	type.SetMatchup("autotroph", 0.5)

	type.Requires.RoleMatch(type.AliasPattern)

end