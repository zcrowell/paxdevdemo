class OpponentMailer < ActionMailer::Base
  default from: Rails.configuration.email_from

  def challenge(opponent_email, player)
    @url = url_for(:host => Rails.configuration.host, :controller => :games, :action => :show, :id => player)
    mail(:to => opponent_email, :subject => 'Abluka Challenge')
  end
end
