function OnRegister(type)

	-- Good attack and defense, but low speed and HP.

	type.SetName("parasite")
	type.SetAliasPattern("parasite")
	type.SetColor(192, 192, 192)

	type.SetBaseHp(40)
	type.SetBaseAtk(50)
	type.SetBaseDef(50)
	type.SetBaseSpd(40)

	type.SetMatchup("predator", 1.5)
	type.SetMatchup("base-consumer", 1.5)

	type.Requires.RoleMatch(type.AliasPattern)

end