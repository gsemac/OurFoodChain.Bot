function register(type)

	type.SetName("autotroph")
	type.SetColor(0, 255, 0)

	type.Requires.RoleMatch("producer|autotroph|plant")

end