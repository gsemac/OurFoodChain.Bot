function OnRegister(move)

	move.SetName("echolocate")
	move.SetDescription("Scans the environment using echolocation. Greatly boosts the user's accuracy.")

	move.SetPP(1)

	move.Requires.DescriptionMatch("\b(echolcation|echolocate)\b")
	
end

function OnMove(args) 

	args.User.Stats.Acc = 1.0

end