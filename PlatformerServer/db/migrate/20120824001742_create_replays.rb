class CreateReplays < ActiveRecord::Migration
  def change
    create_table :replays do |t|
      t.string :player, :null => false
      t.integer :score, :null => false
      t.references :level, :null => false
      t.binary :data, :null => false

      t.timestamps
    end
    add_index :replays, [:level_id, :score]
  end
end
