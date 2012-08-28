require 'matrix'
require 'json'

class Matrix
  def []=(i, j, value)
    @rows[i][j] = value
  end

  def to_json(*m)
    m.to_a.to_json
  end

  def self.json_create(m)
    Matrix.rows(JSON.parse(m))
  end
end
