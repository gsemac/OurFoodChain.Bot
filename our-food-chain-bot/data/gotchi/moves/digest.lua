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

	if(rand(0, 10) == 0) then
		args.Target.Stats.BuffPercent(0.9)
	end

	args.DealDamage()

end