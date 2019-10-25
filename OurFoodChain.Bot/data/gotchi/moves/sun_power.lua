function OnRegister(move)

	move.SetName("Sun Power")
	move.SetDescription("Grows and boosts stats with the help of sunlight.")
	move.SetType("producer")

	move.SetPP(5)

	move.Requires.TypeMatch("producer").MinimumLevel(30)

end

function OnMove(args) 	
	
	if(args.User.StatusName == "shaded") then
	
	args.SetText("but couldn't get any sun")
	
	else
		
		args.User.Stats.BuffPercent(10)
		
		args.SetText("boosting their stats by 10%")

	end

end