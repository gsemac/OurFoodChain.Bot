function OnRegister(move)

	move.SetName("desperation")
	move.SetName("Used as a last resort when no other moves have PP.")

	move.SetPower(40)
	move.SetPP(0)

	-- This move should be inaccessible normally.
	move.Requires.AlwaysFail()

end

function OnMove(args) 
	
	damage = args.CalculateDamage()

	args.User.Stats.Hp = args.User.Stats.Hp - damage

	args.DealDamage()

	args.SetText("lashing out in desperation")
	
end