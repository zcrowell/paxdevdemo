Abluka::Application.routes.draw do
  root :to => 'games#index'
  match 'games/:id/invite' => 'games#invite'
  resources :games
end
