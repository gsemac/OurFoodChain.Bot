function OnRegister(move)

	move.SetName("Digest")
	move.SetDescription("Attacks the opponent with digestive fluids. Has the chance to decrease all of the opponent's stats.")
	move.SetType("decomposer")

	move.SetPower(65)
	move.SetPP(15)

	move.Requires.TypeMatch("decomposer")
	move.Requires.MinimumLevel(40)

end

function OnMove(args) 

	if(Chance(10)) then
		args.Target.Stats.DebuffPercent(10)
	end

	args.DealDamage()

end