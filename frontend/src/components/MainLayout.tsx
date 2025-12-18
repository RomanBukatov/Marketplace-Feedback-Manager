import React, { useState } from 'react';
import { Layout, Menu, theme, Modal, Button } from 'antd';
import {
  DashboardOutlined,
  SettingOutlined,
  RocketOutlined,
  LogoutOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined
} from '@ant-design/icons';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { authService } from '../api/services';

const { Header, Content, Footer, Sider } = Layout;

const MainLayout: React.FC = () => {
  const [collapsed, setCollapsed] = useState(false);
  const {
    token: { colorBgContainer, borderRadiusLG },
  } = theme.useToken();
  
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    Modal.confirm({
      title: 'Выход',
      content: 'Вы действительно хотите выйти из панели управления?',
      okText: 'Да, выйти',
      cancelText: 'Отмена',
      okType: 'danger',
      onOk: async () => {
        try {
          await authService.logout();
          navigate('/login');
        } catch (e) {
          console.error(e);
        }
      }
    });
  };

  const items = [
    { key: '/', icon: <DashboardOutlined />, label: 'Дашборд' },
    { key: '/settings', icon: <SettingOutlined />, label: 'Настройки' },
    { key: 'logout', icon: <LogoutOutlined />, label: 'Выйти', danger: true },
  ];

  const onMenuClick = (e: { key: string }) => {
    if (e.key === 'logout') {
      handleLogout();
    } else {
      navigate(e.key);
      // На мобильных - закрываем меню после клика
      if (window.innerWidth < 992) {
        setCollapsed(true);
      }
    }
  };

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider 
        trigger={null} 
        collapsible 
        collapsed={collapsed}
        breakpoint="lg"
        collapsedWidth="0"
        onBreakpoint={(broken) => {
            if (broken) setCollapsed(true);
        }}
        style={{
          position: 'fixed',
          left: 0,
          top: 0,
          bottom: 0,
          zIndex: 100,
          height: '100vh'
        }}
      >
        <div style={{ 
            height: 32, 
            margin: 16, 
            background: 'rgba(255, 255, 255, 0.2)', 
            borderRadius: 6,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'white',
            fontWeight: 'bold',
            letterSpacing: '2px',
            overflow: 'hidden',
            whiteSpace: 'nowrap'
        }}>
            {collapsed ? '' : 'MFM ADMIN'}
        </div>
        <Menu 
            theme="dark" 
            defaultSelectedKeys={['/']} 
            selectedKeys={[location.pathname]}
            mode="inline" 
            items={items} 
            onClick={onMenuClick} 
        />
      </Sider>
      
      {/* Добавляем отступ слева, если меню открыто (только для десктопа) */}
      <Layout style={{ marginLeft: collapsed ? 0 : 200, transition: 'all 0.2s' }}>
        <Header style={{ padding: 0, background: colorBgContainer, display: 'flex', alignItems: 'center' }}>
            <Button
              type="text"
              icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
              onClick={() => setCollapsed(!collapsed)}
              style={{
                fontSize: '16px',
                width: 64,
                height: 64,
              }}
            />
            <div style={{ flex: 1, display: 'flex', justifyContent: 'space-between', paddingRight: 24, alignItems: 'center' }}>
                <div style={{ fontSize: '18px', fontWeight: 'bold' }}>
                    Marketplace Feedback Manager
                </div>
                <div style={{ color: 'green', display: 'flex', alignItems: 'center', gap: 8 }}>
                     <RocketOutlined /> v2.0
                </div>
            </div>
        </Header>
        <Content style={{ margin: '16px 16px', overflow: 'initial' }}>
          <div
            style={{
              padding: 24,
              minHeight: 360,
              background: colorBgContainer,
              borderRadius: borderRadiusLG,
            }}
          >
            <Outlet />
          </div>
        </Content>
        <Footer style={{ textAlign: 'center', color: '#888' }}>
          Marketplace Feedback Manager ©{new Date().getFullYear()} | Ver 2.0
        </Footer>
      </Layout>
    </Layout>
  );
};

export default MainLayout;