class CreateLevels < ActiveRecord::Migration
  def change
    create_table :levels do |t|
      t.string :name, :null => false
      t.text :content, :null => false

      t.timestamps
    end
  end
end
