import React, { Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';

// Importação dinâmica (Lazy Loading) - O usuário só baixa o JS da página quando acessá-la
const Login = React.lazy(() => import('./pages/Login'));
const Dashboard = React.lazy(() => import('./pages/Dashboard'));
const Agentes = React.lazy(() => import('./pages/Agentes'));
const DetalhesAgente = React.lazy(() => import('./pages/DetalhesAgente'));
const DetalhesDesempenho = React.lazy(() => import('./pages/DetalhesDesempenho'));
const DetalhesPartidas = React.lazy(() => import('./pages/DetalhesPartidas'));
const DetalhesMapas = React.lazy(() => import('./pages/DetalhesMapas'));
const DetalhesRank = React.lazy(() => import('./pages/DetalhesRank'));

function RotaProtegida({ children }) {
  const token = localStorage.getItem('token');
  return token ? children : <Navigate to="/" />;
}

// Componente de carregamento leve para o Suspense
const Loading = () => <div style={{ display: 'flex', justifyContent: 'center', padding: '2rem' }}>Carregando...</div>;

export default function App() {
  return (
    <BrowserRouter>
      <Suspense fallback={<Loading />}>
        <Routes>
          <Route path="/" element={<Login />} />
          
          <Route path="/dashboard" element={<RotaProtegida><Dashboard /></RotaProtegida>} />
          <Route path="/agentes" element={<RotaProtegida><Agentes /></RotaProtegida>} />
          <Route path="/detalhes-agente" element={<RotaProtegida><DetalhesAgente /></RotaProtegida>} />
          <Route path="/detalhes-desempenho" element={<RotaProtegida><DetalhesDesempenho /></RotaProtegida>} />
          <Route path="/detalhes-partidas" element={<RotaProtegida><DetalhesPartidas /></RotaProtegida>} />
          <Route path="/detalhes-mapas" element={<RotaProtegida><DetalhesMapas /></RotaProtegida>} />
          <Route path="/detalhes-rank" element={<RotaProtegida><DetalhesRank /></RotaProtegida>} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  );
}