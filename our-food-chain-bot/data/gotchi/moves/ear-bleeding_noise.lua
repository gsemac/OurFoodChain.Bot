function register(move)
  move.name = "ear-bleeding noise"
  move.description = "Lets out a loud noise that intimidates and disorients the opponent. Only works on opponents with ears."
  
  move.pp = 10
  move.requires.match = "\\b(vocal|cry|shout|scream|bellow|hiss|bark|cries|squeal)"
  move.requires.minLevel = 20
end

function callback(args)
  if args.TargetHasDescription("\\b(ear|hear|listen|echolocate|echolocation)") then
    args.target.stats.atk = args.target.stats.atk * 0.5
    args.target.stats.accuracy = args.target.stats.accuracy * 0.5
  else
    args.text = "but it failed"
  end
end
