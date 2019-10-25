function OnRegister(move)

	move.SetName("Uproot")
	move.SetDescription("Uproots the opponent, eliminating their ability to use recovery moves. Only works on Producers.")
	move.SetType("base-consumer")

	move.SetPower(40)
	move.SetPP(5)

	move.Requires.TypeMatch("base-consumer").MinimumLevel(10)

end

function OnMove(args) 
	
	requirements = NewRequirements()
	requirements.TypeMatch("producer")

	if(args.Target.TestRequirements(requirements)) then
	
		args.target.SetStatus("heal block")

		args.DealDamage()

	else

		args.SetText("but it failed")

	end

end