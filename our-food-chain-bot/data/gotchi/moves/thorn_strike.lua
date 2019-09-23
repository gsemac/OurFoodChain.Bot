function OnRegister(move)

	move.SetName("thorn strike")
	move.SetDescription("Strikes the opponent with thorns, dealing a moderate amount of random base damage.")

	move.SetPP(10)

	move.Requires.MinimumLevel(15).TypeMatch("producer").DescriptionMatch("\\b(?:thorn[sy]?|spike[sy]?|spiky|needles?)\\b")

end

function OnMove(args) 	
	
	args.DealDamage(rand(40, 80))

end