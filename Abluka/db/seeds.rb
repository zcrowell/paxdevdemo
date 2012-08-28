require 'matrix'
Game.create(:board => Matrix.rows([[1, 0, 0, 0, 0], [0, 0, 0, 0, 0], [0, 0, 0, 0, 0], [0, 0, 0, 0, 0], [0, 0, 0, 0, 2]]),
            :player_one => 'AAAAAAAA',
            :player_two => 'BBBBBBBB',
            :current_turn => 1,
            :winner => nil,
            :invite_sent => nil)
