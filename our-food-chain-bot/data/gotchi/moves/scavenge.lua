function OnRegister(move)

	move.SetName("scavenge")
	move.SetDescription("Scavenges for something to eat, restoring a random amount of HP.")
	move.SetType("scavenger")

	move.SetPP(15)

	move.Requires.TypeMatch("scavenger")
	move.Requires.MinimumLevel(10)

end

function OnMove(args) 
	
	amount = rand(0, 6)

	args.RecoverPercent(amount / 10)

	if(amount == 0) then 
		args.SetText("but couldn't find anything")
	end

end