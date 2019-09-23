function onRegister(type)

	-- Autotrophs are slow and hardy.

	type.SetName("producer")
	type.SetAliasPattern("producer|photosynthesis|autotroph|plant")
	type.SetColor(0, 255, 0)

	type.SetBaseHp(45)
	type.SetBaseAtk(48)
	type.SetBaseDef(65)
	type.SetBaseSpd(40)

	type.Requires.RoleMatch(type.AliasPattern)

end