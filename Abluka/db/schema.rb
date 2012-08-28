ActiveRecord::Schema.define(:version => 20120827044022) do

  create_table :games, :force => true do |t|
    t.string   :board, :null => false
    t.string   :player_one, :null => false
    t.string   :player_two
    t.integer  :current_turn, :default => 1
    t.integer  :winner
    t.boolean  :invite_sent, :default => false
    t.datetime :created_at, :null => false
    t.datetime :updated_at, :null => false
  end

  add_index(:games, [:player_one])
  add_index(:games, [:player_two])
end
