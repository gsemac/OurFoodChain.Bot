function OnRegister(move)

	move.SetName("thorn prick")
	move.SetDescription("Pricks the opponent with thorns, dealing a minor amount of random base damage.")

	move.SetPP(20)

	move.Requires.TypeMatch("producer").DescriptionMatch("\\b(?:thorn[sy]?|spike[sy]?|spiky|needles?)\\b")

end

function OnMove(args) 
	
	args.DealDamage(rand(10, 50))

end