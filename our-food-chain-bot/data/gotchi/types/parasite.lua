function onRegister(type)

	-- Good attack and defense, but low speed and HP.

	type.SetName("parasite")
	type.SetColor(192, 192, 192)

	type.SetBaseHp(40)
	type.SetBaseAtk(50)
	type.SetBaseDef(50)
	type.SetBaseSpd(40)

	type.SetMatchup("carnivore", 1.5)
	type.SetMatchup("herbivore", 1.5)

	type.Requires.RoleMatch("parasite")

end